# Configuration Guide

## appsettings.json

Hovedkonfigurasjonsfilen for applikasjonen.

### Sections

#### AccessDb
Konfigurasjon for Access Database tilkobling.

```json
{
  "AccessDb": {
    "Path": "C:\\OResults\\database.mdb",
    "ConnectionString": "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq={Path};"
  }
}
```

- **Path**: Full path til Access Database filen (.mdb eller .accdb)
- **ConnectionString**: ODBC connection string (ikke endre denne med mindre du har spesielle behov)

**Viktig**: Access Database Engine må være installert på maskinen. Last ned fra [Microsoft](https://www.microsoft.com/en-us/download/details.aspx?id=54920).

#### Supabase
Konfigurasjon for Supabase destinasjon.

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ApiKey": "your-service-role-key-here",
    "CompetitionDate": "2024-01-01"
  }
}
```

- **Url**: Supabase project URL (finnes i Supabase Dashboard → Settings → API)
- **ApiKey**: Service role key (finnes i samme sted - **HUSK**: hold denne hemmelig!)
- **CompetitionDate**: Dato for konkurransen i `yyyy-MM-dd` format

**Viktig**: Bruk **service_role** key, ikke anon key, siden dette er en server-side applikasjon.

#### Archive
Konfigurasjon for lokal arkivering.

```json
{
  "Archive": {
    "BasePath": "C:\\ResultsArchive",
    "KeepDays": 90,
    "SploypeCsvEncoding": "windows-1252"
  }
}
```

- **BasePath**: Rotmappe for arkiv. Understrukturen blir automatisk: `BasePath\yyyy\yyyy-MM-dd\`
- **KeepDays**: Hvor mange dager arkivet skal beholdes (ikke implementert ennå)
- **SploypeCsvEncoding**: Encoding for Sploype CSV filer
  - `"windows-1252"` (default): Legacy encoding for World of O WinSplits (anbefalt for nå)
  - `"utf-8-bom"`: Modern UTF-8 med BOM (bruk når World of O støtter UTF-8)

  Se [ENCODING-CONFIGURATION.md](../ENCODING-CONFIGURATION.md) for detaljer om encoding-valg.

#### Transfer
Konfigurasjon for overføringsprosessen.

```json
{
  "Transfer": {
    "IntervalSeconds": 30,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 5,
    "EnableAutoStart": false
  }
}
```

- **IntervalSeconds**: Hvor ofte (i sekunder) systemet skal polle Access DB for oppdateringer
- **RetryAttempts**: Antall forsøk ved feil (ikke implementert ennå)
- **RetryDelaySeconds**: Pause mellom retry-forsøk (ikke implementert ennå)
- **EnableAutoStart**: Start overføring automatisk når applikasjonen starter (ikke implementert ennå)

#### Logging
Konfigurasjon for logging.

```json
{
  "Logging": {
    "LogLevel": "Information",
    "EnableFileLogging": true,
    "LogPath": "C:\\ResultsArchive\\Logs\\application.log"
  }
}
```

- **LogLevel**: Minimum log level (Debug, Information, Warning, Error)
- **EnableFileLogging**: Aktiverer logging til fil (i tillegg til UI)
- **LogPath**: Full path til logg-filen

## Environment-specific Configuration

### appsettings.Development.json
Brukes automatisk når applikasjonen kjører i Development mode.

For å sette environment mode, bruk miljøvariabel:
```
DOTNET_ENVIRONMENT=Development
```

### Environment Variables
Du kan overstyre konfigurasjon med miljøvariabler prefixet med `LIVERESULT_`:

```
LIVERESULT_AccessDb__Path=C:\MyDB\results.mdb
LIVERESULT_Supabase__Url=https://prod.supabase.co
LIVERESULT_Supabase__ApiKey=prod-key-here
```

Merk: Bruk dobbel underscore `__` for å navigere i JSON hierarkiet.

## Testing Configuration

For testing uten ekte databaser:

1. **La AccessDb.Path være tom eller peke til ikke-eksisterende fil**
   - Systemet vil automatisk bruke `MockResultSource` med testdata

2. **La Supabase.Url eller ApiKey være tom**
   - Systemet vil automatisk bruke `FileResultDestination` som skriver til tekstfil

## Troubleshooting

### "Access Database not found"
- Sjekk at Path i appsettings.json er korrekt
- Sjekk at Access Database Engine er installert
- Prøv å åpne databasen i Microsoft Access for å verifisere at den fungerer

### "Supabase connection failed"
- Sjekk at Url er riktig (skal inkludere https://)
- Sjekk at ApiKey er service_role key, ikke anon key
- Test tilkobling i nettleser: `https://your-project.supabase.co/rest/v1/`

### "Archive directory access denied"
- Sjekk at applikasjonen har skrivetilgang til BasePath
- Prøv å kjøre som administrator (midlertidig test)
- Velg en annen mappe som brukeren har tilgang til

## Security Best Practices

⚠️ **VIKTIG**: appsettings.json inneholder sensitive data!

1. **Ikke commit appsettings.json til git** med ekte API keys
2. Bruk appsettings.Development.json for lokal utvikling
3. Bruk miljøvariabler for produksjonssetting
4. Vurder å kryptere Supabase ApiKey (fremtidig feature)
5. Beskytt tilgang til konfigurasjonsfiler på server

## Example: Production Deployment

For produksjonssetting, bruk miljøvariabler i stedet for appsettings.json:

```powershell
# PowerShell
$env:LIVERESULT_Supabase__ApiKey = "prod-key-from-secure-vault"
.\o-bergen.LiveResultManager.exe
```

```bash
# Linux/Mac (if cross-platform in future)
export LIVERESULT_Supabase__ApiKey="prod-key-from-secure-vault"
dotnet o-bergen.LiveResultManager.dll
```

## Delta Detection & Automatic Cleanup

Systemet implementerer automatisk delta-deteksjon for å håndtere tilfeller hvor deltakere er fjernet fra kilden (f.eks. feilregistreringer):

### Hvordan det fungerer

1. **Første overføring**: Alle resultater fra kilden sendes til destinasjonen
2. **Påfølgende overføringer**: 
   - Systemet sammenligner nye resultater med forrige snapshot
   - Identifiserer deltakere som har forsvunnet fra kilden
   - Sletter automatisk disse fra Supabase basert på ID
3. **Snapshot**: Systemet lagrer alltid siste resultatsett i minnet for sammenligning

### Eksempel scenario

**Scenario**: En deltaker er feilregistrert med ID "123" og havner i første overføring til Supabase.

1. **Transfer 1**: ID "123" sendes til Supabase ✅
2. **I OE/Access**: Admin fjerner feilregistreringen (ID "123" forsvinner)
3. **Transfer 2**: 
   - Systemet detekterer at ID "123" mangler i nye resultater
   - Sletter automatisk ID "123" fra Supabase
   - Logger: `🗑️ Detected 1 removed results, deleting from destination...`

### Logging

Delta-deteksjon genererer følgende logmeldinger:

```
🗑️ Detected 2 removed results, deleting from destination...
✅ Deleted 2 results from Supabase
```

Ved feil:
```
⚠️ Warning: Failed to delete removed results: [error message]
```

### Metadata

Antall slettede records blir lagret i metadata:

```json
{
  "recordsRead": 150,
  "recordsWritten": 148,
  "recordsDeleted": 2,
  "success": true
}
```

### Viktig å vite

- ⚠️ Delta-deteksjon kjører **kun** mot forrige snapshot i minnet
- 🔄 Ved restart av applikasjonen resettes snapshotet (første transfer etter restart vil ikke slette noe)
- 🔑 Sletting baseres på `Id`-feltet fra kilden
- 📊 Slettede records blir ikke arkivert separat, men logget i metadata

## Unique Key & Upsert Behavior

Systemet bruker **composite key** (`id` + `competition_date`) som primærnøkkel for Supabase upsert:

### Hvordan det fungerer

1. **Composite Nøkkel**: `id` + `competition_date` (unikt per deltaker per løp)
2. **Upsert**: Hvis kombinasjonen `(id, competition_date)` allerede finnes, oppdateres raden
3. **Insert**: Hvis kombinasjonen er ny, legges en ny rad til

⚠️ **KRITISK**: Uten `competition_date` i nøkkelen vil forskjellige løp overskrive hverandres resultater!

### Eksempel scenario - Innen samme løp

**Scenario**: En løper med ID "9870" endrer status fra "Startet" til "OK" i løp 2026-04-08

```
Transfer 1: id=9870, competition_date=2026-04-08, Status=S, Time="" → Supabase (INSERT)
Transfer 2: id=9870, competition_date=2026-04-08, Status=OK, Time="45:23" → Supabase (UPDATE)
```

**Resultat**: Samme rad oppdateres med ny status og tid.

### Eksempel scenario - Forskjellige løp

**Scenario**: Samme løper (ID "9870") deltar i to forskjellige løp

```
Løp 1: id=9870, competition_date=2026-04-08 → Supabase (INSERT rad 1)
Løp 2: id=9870, competition_date=2026-04-15 → Supabase (INSERT rad 2)
```

**Resultat**: To separate rader i Supabase, ett resultat per løp.

### Viktig

- 🔑 **Composite Primærnøkkel**: `(id, competition_date)` - begge felter inngår
- 📅 **competition_date** må ALLTID settes før WriteResultsAsync kalles
- 📝 **eCard** kan være tom eller lik for flere deltakere (ikke nøkkel)
- ✅ Alle resultater fra Access DB sendes til Supabase (ingen deduplikering)
- 🔄 Samme ID i forskjellige løp er OK - de får separate rader

## Poeng (Ranking Points)

**VIKTIG:** Poeng leses direkte fra Access DB uten modifikasjon.

### Hvordan det fungerer

1. **Access DB**: Kolonne `points` i `Name`-tabellen
2. **Applikasjon**: Leser `points` og sender til Supabase UTEN beregning
3. **Supabase**: Lagrer poeng eksakt som i Access DB

### Arbeidsflyt for poeng

1. **Etter løpet**: Resultater lastes inn i Access DB
2. **Juster for ugyldige strekk**: Manuelt eller via applikasjonen
3. **Sett poeng**: Manuelt i Access DB basert på justerte tider
4. **Kjør applikasjon**: Poeng overføres eksakt som de er

### ⚠️ Tidligere versjon (før v2.0)

Tidligere versjoner beregnet poeng automatisk basert på formelen:
```
points = (vinnertid / løpertid) * 1000
```

Dette er nå **DEAKTIVERT** fordi:
- Poeng skal settes etter justering for ugyldige strekk
- Automatisk beregning overskrev manuelt satte verdier
- Status != 'A' fikk automatisk blanke poeng

### Hvis du vil ha automatisk beregning tilbake

I `ResultTransferService.cs`, fjern kommentar på linje 266-275:
```csharp
if (results.Count > 0)
{
    Log("📊 Calculating ranking points per class...", LogLevel.Information);
    var pointsCalculated = CalculateRankingPoints(results);
    ...
}
```

**ADVARSEL:** Dette vil overskrive poeng fra Access DB!
