# Verifisering av Poeng (Points) for Løp 2

## Problem
Ranking-poengene ser ikke ut til å være korrekte for løp 2 (2026-04-15).

## Mulige årsaker

### 1. **Data ble overskrevet før fix** (mest sannsynlig)
Før primary key-fixen:
- Løp 2 overskrev løp 1
- Hvis løp 1 hadde andre poeng-verdier, kan disse fortsatt være i databasen
- Når løp 2 ble kjørt på nytt etter fixen, kan gamle poeng ha blitt beholdt

### 2. **Access DB har ikke poeng-data**
- Sjekk at `points`-kolonnen i Access DB faktisk har verdier
- Kan være NULL eller tom string

### 3. **Mapping-problem**
- Points mappes som string, kan være format-problem

## Verifisering

### Steg 1: Sjekk poeng i Supabase for løp 2

```sql
-- Se alle poeng for løp 2
SELECT 
    id, 
    "firstName", 
    "lastName", 
    class,
    points,
    competition_date,
    updated_at
FROM live_results 
WHERE competition_date = '2026-04-15'
ORDER BY class, points DESC
LIMIT 50;
```

**Hva å se etter:**
- Er `points` = '0' eller tom string for alle?
- Er `points` korrekte tall?
- Sammenlign med løp 1:

```sql
-- Sammenlign poeng mellom løp 1 og løp 2
SELECT 
    competition_date,
    COUNT(*) as totalt,
    COUNT(CASE WHEN points = '0' OR points = '' THEN 1 END) as uten_poeng,
    COUNT(CASE WHEN points != '0' AND points != '' THEN 1 END) as med_poeng
FROM live_results 
GROUP BY competition_date
ORDER BY competition_date;
```

### Steg 2: Sjekk Access DB direkte

Åpne Access-databasen for løp 2 og kjør:

```sql
SELECT id, name, ename, class, points 
FROM Name 
WHERE status IN ('A','D','B','S')
ORDER BY class, points DESC;
```

**Sammenlign:**
- Er poeng fylt ut i Access DB?
- Matcher verdiene det du forventer?

### Steg 3: Test at mapping fungerer

Kjør applikasjonen med debug/logging for å se hva som leses:

```powershell
# I SupabaseResultDestination.cs, legg til logging i MapToLiveResult
# (midlertidig for testing)
```

## Løsninger

### Løsning 1: Slett og kjør på nytt (anbefalt)

```sql
-- Slett alle resultater for løp 2
DELETE FROM live_results 
WHERE competition_date = '2026-04-15';

-- Verifiser at de er borte
SELECT COUNT(*) FROM live_results WHERE competition_date = '2026-04-15';
-- Skal gi 0
```

Deretter kjør applikasjonen på nytt mot løp 2 sin Access DB.

### Løsning 2: Tving oppdatering av eksisterende data

Hvis du vet at Access DB har korrekte poeng:

```sql
-- Backup først!
CREATE TABLE live_results_temp AS 
SELECT * FROM live_results WHERE competition_date = '2026-04-15';

-- Slett og kjør app på nytt
DELETE FROM live_results WHERE competition_date = '2026-04-15';
```

### Løsning 3: Manuell SQL-oppdatering (hvis kun poeng mangler)

**IKKE anbefalt** - bedre å kjøre appen på nytt, men hvis du absolutt må:

```sql
-- Eksempel: oppdater poeng for en spesifikk person
UPDATE live_results 
SET points = '150'
WHERE id = '123' AND competition_date = '2026-04-15';
```

## Anbefalt fremgangsmåte

1. **Verifiser problemet:**
   ```sql
   SELECT competition_date, points, COUNT(*) 
   FROM live_results 
   GROUP BY competition_date, points
   ORDER BY competition_date, points;
   ```

2. **Slett løp 2 data:**
   ```sql
   DELETE FROM live_results WHERE competition_date = '2026-04-15';
   ```

3. **Kjør app på nytt mot løp 2:**
   - Sett `AccessDbPath` til løp 2 sin .mdb fil
   - Start applikasjonen
   - Vent til all data er overført

4. **Verifiser at poeng er korrekte:**
   ```sql
   SELECT id, "firstName", "lastName", class, points
   FROM live_results 
   WHERE competition_date = '2026-04-15'
   ORDER BY class, CAST(points AS INTEGER) DESC
   LIMIT 20;
   ```

## Debugging

Hvis problemet fortsetter etter re-import:

### Test at Points leses fra Access DB

Legg til logging i `AccessDbResultSource.cs` linje 230:

```csharp
Points = GetString(reader, "points"),
// Midlertidig logging:
// Console.WriteLine($"DEBUG: ID={result.Id}, Points={result.Points}");
```

### Test at Points sendes til Supabase

Legg til logging i `SupabaseResultDestination.cs` linje 213:

```csharp
Points = result.Points,
// Midlertidig logging:
// Console.WriteLine($"DEBUG: Mapping ID={result.Id}, Points={result.Points} for {_competitionDate}");
```

## Konklusjon

Mest sannsynlig årsak: **Data fra løp 1 ble ikke riktig overskrevet** når løp 2 ble kjørt inn på nytt.

**Løsning:** Slett løp 2 fra Supabase og kjør inn på nytt.

Dette bør løse problemet siden koden nå har riktig composite key og OnConflict.
