# Duplikathåndtering – Utviklingslogg og Arkitekturminne

> Formål: Denne filen dokumenterer design-beslutninger, implementerte funksjoner og viktige tekniske detaljer
> rundt duplikatdeteksjon og sammenslåing av løpere. Brukes som kontekst for fremtidige forbedringer.

---

## Bakgrunn og problemstilling

Tidtakersystemet eTiming lagrer løpere lokalt i en Access-database (`.mdb`/`.accdb`).
Resultater synkroniseres til Supabase (`live_results`-tabellen) og vises på web.

**Kjerneproblemet:** Arrangører oppretter noen ganger en ny løper i eTiming i stedet for å melde
på en eksisterende. Dette medfører:
- Duplikat-ID-er i `Name`-tabellen
- Splittet løpshistorikk i `multi`-tabellen (lokalt) og `live_results` (Supabase)
- Feil klasse, brikkenummer, navn m.m. på feil ID

**Endgame:** Én fysisk person = én løper-ID. All historikk (inkl. 18 sesonger) koblet til den ene ID-en.

---

## Dataarkitektur

### Lokalt (Access DB via ODBC)

| Tabell | Nøkkel | Relevante felt |
|--------|--------|----------------|
| `Name` | `id` (PK) | `id`, `name` (fornavn), `ename` (etternavn), `ecard`, `class` |
| `multi` | `id + day` (sammensatt PK) | `id`, `day` (løpsnummer i sesong) |

> ⚠️ ODBC-parametere er **posisjonelle** (`?`), ikke navngitte. Rekkefølgen i `AddWithValue` er kritisk.
>
> ⚠️ `Name.id` er **AutoNumber** i Access — kan *aldri* oppdateres via ODBC.
>
> ⚠️ `multi.id` er del av **sammensatt PK (`id`, `day`)** — kan ikke `UPDATE`-es via ODBC (feil `23000`). Bruk alltid `DELETE + INSERT` for å «flytte» en rad.

### Supabase

| Tabell | Nøkkel | Relevante felt |
|--------|--------|----------------|
| `live_results` | `id + competition_date` (sammensatt PK) | `id`, `competition_date`, `runner_id`, osv. |
| `runners` | `id` (løper-UUID, PK) | `runner_id` (matcher lokal `id`) |
| `profiles` | `id` (Supabase auth UUID, PK) | `runner_id` (FK → runners) |

> `runners` er et **komplett historisk register** over alle løper-ID-er på tvers av 18 sesonger.
> Det er *ikke* begrenset til de med nettkontoer.
>
> `profiles` er brukerkontotabellen. Feltet `runner_id` er en FK som brukeren selv har valgt
> fra en liste (basert på mobilnummeroppslag + fuzzy name matching mot historikken).
> Tilstedeværelse i `profiles` er det **sterkeste signalet** for hvilken ID som er kanonisk
> for en person — dette er ID-en man bør samle seg rundt ved sammenslåing.

---

## Implementerte funksjoner

### 1. Duplikatdeteksjon (fuzzy matching)

**Fil:** `UI/DuplicatesForm.cs` (privat metode `FindDuplicatePairs`)

- Henter alle deltakere fra `Name` via `AccessDbResultSource.FetchAllParticipantsAsync`
- Sammenligner navn med **Levenshtein-likhetsgrad**:
  - Vekting: fornavn 45 %, etternavn 55 %
  - Sjekker også byttet rekkefølge (fornavn/etternavn forvekslet)
  - Terskel: **0.75** (75 % likhet)
- Slår opp løpshistorikk per ID i Supabase (`FetchCompetitionsByIdsAsync`)
- Slår opp om ID finnes i `runners`-registeret (`FetchRunnerIdStatusAsync`)
- Slår opp om ID er valgt som profil-ID i `profiles` (`FetchProfileRunnerIdsAsync`)

### 2. UI – DuplicatesForm

**Fil:** `UI/DuplicatesForm.cs`

Kolonner i `DataGridView` (i rekkefølge):

| Navn | Type | Beskrivelse |
|------|------|-------------|
| `Merge` | CheckBox | Merk par for sammenslåing |
| `TransferInfo` | CheckBox | Bruk ID-bytting (overfør info fra ny til gammel ID) |
| `C1Id` | Text | ID på deltaker #1 (lavest = beholdes) |
| `C1Konto` | Text | «✓ Konto» (grønn) hvis #1 finnes i `runners`-registeret |
| `C1Profil` | Text | «★ Profil» (blå) hvis #1 er valgt som profil-ID i `profiles` |
| `C1First/Last/Class/ECard` | Text | Deltakerinfo #1 |
| `C1Lop` | Text | Løpsdatoer fra `live_results` for #1 |
| `C2Id` .. `C2Lop` | Text | Tilsvarende for deltaker #2 (inkl. C2Konto, C2Profil) |
| `Score` | Text | Fuzzy-likhetsgrad |

**Radfarging:**
- 🔴 Rød (`#FFDC DC`) – begge har oppføring i `runners` (høy risiko)
- 🟡 Gul (`#FFFAD2`) – én av dem finnes i `runners`
- Hvit – ingen konto/oppføring

**Cellefarging:**
- `C1/C2Konto`: Grønn fet skrift for «✓ Konto» (finnes i `runners`)
- `C1/C2Profil`: Blå fet skrift for «★ Profil» (valgt som profil-ID i `profiles`)

### 3. Standard sammenslåing

**Laveste ID beholdes.** Høyeste ID fjernes.

**Access DB** (`AccessDbResultSource.MergeDuplicateAsync`):
1. Les hvilke `day`-verdier `keepId` allerede dekker
2. For dager `keepId` ikke har: `CopyMultiRowAsync` — leser alle kolonner fra `multi`-raden dynamisk, INSERT med `id=keepId`, DELETE original
3. DELETE resterende `removeId`-rader fra `multi`
4. DELETE `removeId` fra `Name`

**Supabase** (`SupabaseLookupService.ReassignIdAsync`):
- For hver rad med `oldId`: DELETE + INSERT med `newId` (upsert på sammensatt PK)

**runners-tabell:**
- Hvis `removeId` har konto → `UpdateRunnerIdAsync(removeId, keepId)`

### 4. «Overfør fra ny» (data-kopi, ikke ID-bytte)

Brukes når duplikatet (høyere ID) har **korrekt oppdatert info** (ny klasse, ny brikke osv.)
men vi ønsker å **beholde det gamle løpernummeret**.

**Viktig begrensning:** `Name.id` er AutoNumber i Access og kan ikke oppdateres. ID-bytte
er fysisk umulig lokalt. I stedet kopieres deltakerdata fra `removeId` til `keepId`s `Name`-rad.

**`Name`-tabellen er identitets-master** — den inneholder både registreringsdata (hvem personen er)
og løpsresultat-data (hva de presterte i dag). Disse behandles ulikt ved sammenslåing:

| Felttype | Eksempler | Håndtering |
|----------|-----------|------------|
| **Identitet / registrering** | `name`, `ename`, `ecard`, `ecard2`, `class`, `cource`, `team`, `sex`, `born`, `bib`, `nation` m.fl. | **Kopieres alltid** fra `removeId` til `keepId` |
| **Løpsresultat** | `status`, `statusmsg`, `starttime`, `times`, `points` | **Kopieres bare** hvis `keepId` ikke har et resultat ennå |

`keepHasResult = true` hvis `keepId.status ∈ {A, D, B, S}` (dvs. løpet er gjennomført, DQ, DNF eller i skogen).

**Access DB** (`AccessDbResultSource.SwapMergeDuplicateAsync(keepId, removeId)`):
- Fase 1: Les `SELECT *` fra *begge* `Name`-rader dynamisk (`removeId` og `keepId`)
- Fase 2: Bestem `keepHasResult` basert på `keepId.status`
- Fase 3: Bygg SET-liste: identitetsfelt alltid; løpsresultatfelt bare hvis `!keepHasResult`
- Fase 4: `UPDATE Name SET [utvalgte felt] = removeId-verdier WHERE id = keepId`
- Fase 5: Merge `multi` med `CopyMultiRowAsync` (ikke-konflikterende dager)
- Fase 6: DELETE `removeId` fra `multi` og `Name`

**Supabase** (`SupabaseLookupService.ReassignIdAsync(removeId, keepId)`):
- Identisk med standard merge — `live_results.id` er varchar (ikke AutoNumber), men swap er unødvendig
- `removeId`s race-rader flyttes til `keepId`; `keepId`s historikk beholdes urørt

**runners-tabell:**
- Hvis `removeId` har konto → `UpdateRunnerIdAsync(removeId, keepId)`

**Resultat:** `keepId` beholder løpernummer + gammel historikk, men får `removeId`s
navn, klasse, brikke og alle andre `Name`-felt.

### 5. Konto-sikkerhetsmekanismer

- Hvis **begge** i et par har konto: ekstra advarselsdialog med forklaring om 18 sesongers historikk
- Advarselstekst i oppsummeringslisten: `⚠ BEGGE HAR KONTO`
- Informasjonstekst: `ℹ konto flyttes fra ny til gammel ID` / `ℹ konto beholdes på gammel ID`

---

## Filkart

```
Infrastructure/
  Sources/
    AccessDbResultSource.cs       ← FetchAllParticipantsAsync, MergeDuplicateAsync,
                                     FindSafeTempIdAsync, SwapMergeDuplicateAsync
  Destinations/
    SupabaseLookupService.cs      ← FetchCompetitionsByIdsAsync, ReassignIdAsync,
                                     SwapReassignIdsAsync, FetchRunnerIdStatusAsync,
                                     FetchProfileRunnerIdsAsync, UpdateRunnerIdAsync
    Runner.cs                     ← Supabase-modell for runners-tabellen (historisk register)
    Profile.cs                    ← Supabase-modell for profiles-tabellen (brukerkontoer)
    LiveResult.cs                 ← Supabase-modell for live_results (sammensatt PK)
UI/
  DuplicatesForm.cs               ← Hele duplikat-UI med fuzzy matching og merge-logikk
Core/
  Models/
    ParticipantEntry.cs           ← record(Id, FirstName, LastName, ECard, Class)
```

---

## Kjente begrensninger og fremtidige forbedringer

### Mulige forbedringer

- [ ] **Manuell overstyring av «keep»-ID** – i dag velges alltid laveste ID. Bør kunne overstyres,
      særlig når den med høyest ID har en aktiv `profiles`-oppføring (sterkeste kanonisk signal).
- [ ] **Automatisk foreslå riktig «keep»-ID** basert på `profiles`-tilstedeværelse – hvis en av
      to duplikater finnes i `profiles`, bør den ID-en foreslås som «keep» uavhengig av tallverdi.
- [ ] **Batch-lookup optimalisering** – `FetchRunnerIdStatusAsync` gjør én Supabase-spørring per ID.
      Bør bruke `IN`-filter når Supabase C#-klienten støtter det godt.
- [ ] **Undologg** – logg alle gjennomførte sammenslåinger til en lokal fil for eventuell manuell reversering.
- [ ] **runners.runner_id ved begge-har-konto** – i dag oppdateres `removeId`s konto til `keepId`,
      men `keepId`s konto blir liggende uendret. Ved ID-bytting kan dette skape to `runners`-rader
      med samme `runner_id`. Bør detekteres og håndteres eksplisitt.
- [ ] **Threshold som konfigurerbart parameter** – 0.75 er hardkodet. Kan eksponeres i UI.
- [ ] **Vis antall løp / løpsdetaljer** i tooltip istedenfor bare datoer i «Løp»-kolonnen.
- [ ] **Filtrer på klasse** – vis bare duplikater innen samme klasse for å redusere støy.

### Kjente tekniske hensyn

- `Name.id` er **AutoNumber** i Access — kan aldri oppdateres via ODBC. ID-bytte er umulig lokalt.
- `multi.id` er del av **sammensatt PK (`id`, `day`)** — ODBC blokkerer UPDATE med `ERROR 23000`. Bruk alltid `CopyMultiRowAsync` (DELETE + INSERT med dynamisk kolonnelisting).
- `CopyMultiRowAsync` leser alle kolonner dynamisk med `SELECT *` — robust mot skjemaendringer i eTiming.
- Supabase `live_results` har **sammensatt PK (`id`, `competition_date`)** — bruk alltid delete+upsert for ID-endringer.
- ODBC parametre er **posisjonelle** — navngiving i `AddWithValue` ignoreres, kun rekkefølge teller.
- `lblStatus` i `DuplicatesForm` må ha `Width ≤ 860` — `btnFind` starter på `x=882`.
- `SwapReassignIdsAsync` og `FindSafeTempIdAsync` finnes i kodebasen men brukes ikke lenger.
