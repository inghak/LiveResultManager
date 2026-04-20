# 🚨 KRITISK DATABASE-FIX PÅKREVD

## Problem Identifisert
Resultater fra forskjellige løp har blitt overskrevet fordi databasen mangler riktig primary key constraint.

**Eksempel på feilen:**
- Løp 1 (2026-04-08): Deltaker id="123" 
- Løp 2 (2026-04-15): Deltaker id="123"
- **RESULTAT**: Løp 2 overskrev data fra Løp 1 ❌

## Årsak
Primary key var satt til bare `id`, ikke `(id, competition_date)`.

## Løsning

### Steg 1: Sjekk gjeldende constraint i Supabase

Kjør denne SQL-en i Supabase SQL Editor for å se gjeldende primary key:

```sql
SELECT 
    tc.constraint_name, 
    kcu.column_name,
    tc.constraint_type
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu 
    ON tc.constraint_name = kcu.constraint_name
WHERE tc.table_name = 'live_results' 
    AND tc.constraint_type = 'PRIMARY KEY';
```

### Steg 2: Hvis primary key kun er 'id', må den endres

**⚠️ VIKTIG: Ta backup av data først!**

```sql
-- 1. BACKUP: Eksporter alle data
CREATE TABLE live_results_backup AS 
SELECT * FROM live_results;

-- 2. Sjekk for duplikater som vil gi konflikt
-- Dette viser om samme ID finnes i flere competition_date
SELECT id, competition_date, COUNT(*) 
FROM live_results 
GROUP BY id, competition_date 
HAVING COUNT(*) > 1;

-- 3. Dropp gammel primary key constraint
ALTER TABLE live_results 
DROP CONSTRAINT live_results_pkey;

-- 4. Legg til ny composite primary key
ALTER TABLE live_results 
ADD PRIMARY KEY (id, competition_date);
```

### Steg 3: Verifiser at constraint er korrekt

```sql
-- Sjekk at composite key er på plass
SELECT 
    tc.constraint_name, 
    kcu.column_name,
    kcu.ordinal_position
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu 
    ON tc.constraint_name = kcu.constraint_name
WHERE tc.table_name = 'live_results' 
    AND tc.constraint_type = 'PRIMARY KEY'
ORDER BY kcu.ordinal_position;
```

**Forventet resultat:**
```
constraint_name    | column_name       | ordinal_position
-------------------|-------------------|------------------
live_results_pkey  | id                | 1
live_results_pkey  | competition_date  | 2
```

### Steg 4: Test at det fungerer

```sql
-- Test: Prøv å legge inn samme ID i to forskjellige løp
-- Dette skal LYKKES (to separate rader)
INSERT INTO live_results (id, competition_date, firstName, lastName, class) 
VALUES ('TEST123', '2026-04-08', 'Test', 'Person', 'H21');

INSERT INTO live_results (id, competition_date, firstName, lastName, class) 
VALUES ('TEST123', '2026-04-15', 'Test', 'Person', 'H21');

-- Sjekk at begge finnes
SELECT id, competition_date, firstName FROM live_results 
WHERE id = 'TEST123';

-- Rydd opp test-data
DELETE FROM live_results WHERE id = 'TEST123';
```

## Status Etter Fixes

✅ C#-kode er oppdatert med:
- Composite primary key: `[PrimaryKey("id", false)]` og `[PrimaryKey("competition_date", false)]`
- OnConflict endret til: `.OnConflict("id,competition_date")`

⚠️ **DATABASE må også oppdateres** - se stegene over

## Data Recovery (hvis nødvendig)

Hvis data fra løp 1 (2026-04-08) ble overskrevet:

1. Sjekk om dere har backup av Access-databasen for løp 1
2. Kjør applikasjonen på nytt mot løp 1 sin Access-database
3. Resultater vil nå få riktig `competition_date` og ikke overskrive løp 2

## Preventive Tiltak

For å unngå dette i fremtiden:
- ✅ Alltid sett riktig `competition_date` før opplasting
- ✅ Composite key hindrer nå overskrivning mellom løp
- ✅ Test med forskjellige datoer før produksjon
- ✅ Verifiser at data er korrekt etter hver opplasting

## Kontakt

Hvis du har spørsmål om denne fixen, sjekk:
- `o-bergen.LiveResultManager/Configuration/README.md` - oppdatert dokumentasjon
- `o-bergen.LiveResultManager/Infrastructure/Destinations/SupabaseResultDestination.cs` - kildekode
