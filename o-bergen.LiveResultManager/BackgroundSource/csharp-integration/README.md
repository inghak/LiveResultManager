# C# Integration for Live Results

Dette er eksempel-kode for å integrere ET2002/Emit-systemet med live resultater i Supabase.

## Forutsetninger

1. .NET 6.0 eller nyere
2. NuGet-pakker:
   - `Supabase` (latest version)
   - `Newtonsoft.Json` eller `System.Text.Json`

## Installasjon

```bash
dotnet add package Supabase
dotnet add package Newtonsoft.Json
```

## Konfigurasjon

Opprett `appsettings.json`:

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ServiceRoleKey": "your-service-role-key-here",
    "CompetitionDate": "2026-04-09"
  },
  "EmitSystem": {
    "ResultsJsonPath": "C:\\ET2002\\results.json",
    "WatchInterval": 5000
  }
}
```

**VIKTIG:** Service Role Key må holdes hemmelig! Ikke commit til Git.

## Bruk

### 1. Enkel upload (engangslast)

```csharp
var service = new LiveResultsService();
await service.InitializeAsync();

// Last opp alle dagens resultater
await service.UploadAllResultsAsync();
```

### 2. Kontinuerlig overvåking (anbefalt)

```csharp
var service = new LiveResultsService();
await service.InitializeAsync();

// Start overvåking av results.json
await service.StartWatchingResultsFileAsync();

// Holder programmet kjørende
Console.WriteLine("Trykk en tast for å stoppe...");
Console.ReadKey();
```

## Filbeskrivelser

- **LiveResult.cs**: Datamodell som matcher `live_results` tabellen
- **LiveResultsService.cs**: Hovedlogikk for upload til Supabase
- **EmitResult.cs**: Modell for ET2002/Emit JSON-format
- **Program.cs**: Eksempel på bruk

## Testing

Kjør med test-data:

```bash
dotnet run --project LiveResultsUploader
```

## Feilsøking

### Feilmelding: "Unauthorized"
- Sjekk at Service Role Key er korrekt i `appsettings.json`
- Kontroller at Supabase prosjekt-URL er riktig

### Feilmelding: "Table 'live_results' does not exist"
- Kjør database migration i Supabase SQL Editor: `supabase-migrations/007-live-results.sql`
- Kontroller at du er tilkoblet riktig Supabase-prosjekt
- Verifiser at tabellen finnes: Database → Tables → live_results

### Resultater vises ikke på nettsiden
- Sjekk at `competition_date` matcher løpets dato (format: YYYY-MM-DD)
- Verifiser at resultater er synlige i Supabase Dashboard → Table Editor → live_results

## Ytelsestips

1. **Batch upload**: Bruk `BatchUpsertResultsAsync()` for å laste opp flere resultater samtidig
2. **Unngå duplikater**: UNIQUE constraint på (competition_date, ecard, class) sikrer at samme resultat ikke lastes opp flere ganger
3. **Cleanup**: Gamle resultater slettes automatisk etter 1 dag via `cleanup_old_live_results()` funksjonen

## Sikkerhet

⚠️ **ALDRI commit Service Role Key til versjonskontroll!**

Legg til i `.gitignore`:
```
appsettings.json
appsettings.*.json
```

Bruk miljøvariabler i produksjon:
```csharp
var serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
```
