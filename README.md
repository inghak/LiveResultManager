# Live Result Manager

**Automatisk overføring av orienteringsløp-resultater fra EQTiming til Supabase.**

![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![Windows](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## 📋 Beskrivelse

Live Result Manager er en Windows-applikasjon som automatisk overfører direkteresultater fra EQTiming Access-database til Supabase for sanntidsvisning på web. Utviklet for Orientering Bergen.

### Hovedfunksjoner

- ✅ **Automatisk polling** av EQTiming Access-database
- ✅ **Sanntidsopplasting** til Supabase
- ✅ **Delta-deteksjon** - sletter fjernede resultater
- ✅ **Automatisk arkivering** til JSON
- ✅ **Auto-start ved Windows oppstart**
- ✅ **GUI for enkel konfigurasjon**

## 🚀 Kom i gang

### Systemkrav

- **OS:** Windows 10/11 (64-bit)
- **.NET Runtime:** .NET 10 (inkludert i self-contained build)
- **Database Driver:** Microsoft Access Database Engine 2016 Redistributable

### Installasjon

#### 1. Last ned siste release

```powershell
# Eller last ned fra GitHub Releases
```

#### 2. Kjør installasjonsskriptet (som Administrator)

```powershell
.\install.ps1
```

Dette vil:
- Kopiere filer til `C:\LiveResultManager`
- La deg konfigurere innstillinger
- Sette opp auto-start ved oppstart

#### 3. Manuell installasjon

Hvis du ikke bruker `install.ps1`:

1. Pakk ut til ønsket mappe
2. **Konfigurer secrets** - se [CONFIGURATION.md](CONFIGURATION.md) for detaljert guide:

```powershell
# Copy example configuration
Copy-Item o-bergen.LiveResultManager\appsettings.example.json o-bergen.LiveResultManager\appsettings.json

# Edit appsettings.json with your actual Supabase credentials
```

3. Kjør `LiveResultManager.exe`

## ⚙️ Konfigurasjon

**⚠️ IMPORTANT: Never commit secrets to git!**

See [CONFIGURATION.md](CONFIGURATION.md) for complete configuration guide including:
- How to set up Supabase credentials securely
- Using environment variables
- Using .NET User Secrets
- Security best practices

### Access Database

Installer først [Microsoft Access Database Engine 2016](https://www.microsoft.com/en-us/download/details.aspx?id=54920) (64-bit).

Sett sti til EQTiming-databasen i `appsettings.json`:

```json
"AccessDbPath": "C:\\EQTiming\\Results.mdb"
```

**Note:** For Supabase and other secrets, see [CONFIGURATION.md](CONFIGURATION.md)

### Auto-start ved oppstart

Bruk `setup-autostart.ps1` (krever Administrator):

```powershell
# Task Scheduler (anbefalt)
.\setup-autostart.ps1 -Method TaskScheduler -ExePath "C:\LiveResultManager\LiveResultManager.exe"

# Startup Folder (enklest)
.\setup-autostart.ps1 -Method StartupFolder -ExePath "C:\LiveResultManager\LiveResultManager.exe"

# Fjern auto-start
.\setup-autostart.ps1 -Method TaskScheduler -Remove
```

Se [DEPLOYMENT.md](DEPLOYMENT.md) for detaljerte instruksjoner.

## 📦 Bygging fra kildekode

### Forutsetninger

- Visual Studio 2022 eller nyere
- .NET 10 SDK

### Bygg

```powershell
# Self-contained executable (anbefalt)
.\build-production.ps1

# Eller manuelt
dotnet publish o-bergen.LiveResultManager/o-bergen.LiveResultManager.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish
```

Output finnes i `./publish/`

## 🏗️ Arkitektur

Prosjektet følger **Clean Architecture** med tydelig lagdeling:

```
o-bergen.LiveResultManager/
├── Core/                    # Domain layer (ingen dependencies)
│   ├── Models/              # Domain models
│   ├── Interfaces/          # Abstractions
│   └── Services/            # Business logic
├── Application/             # Application layer
│   ├── DTOs/                # Data transfer objects
│   └── Mappers/             # Object mapping
├── Infrastructure/          # External concerns
│   ├── Sources/             # Data sources (AccessDb, Mock)
│   ├── Destinations/        # Data destinations (Supabase, File)
│   ├── Archive/             # Archiving
│   └── Logging/             # Logging
└── Configuration/           # DI & setup
```

### Repository Pattern

- **IResultSource** - Abstraction for data sources
- **IResultDestination** - Abstraction for destinations
- **IResultArchive** - Abstraction for archiving

Dette gjør det enkelt å bytte mellom kilder/destinasjoner uten kodeendringer.

## 🧪 Testing

```powershell
# Kjør med Mock-data
# Sett "SourceType": "Mock" i appsettings.json

# Test connection
dotnet run -- --test-connection
```

## 📝 Logging

Logger skrives til:
- `logs/transfer-YYYY-MM-DD.log`
- Windows Event Viewer (hvis kjørt som service)

Logg-nivåer:
- `Information` - Normal drift
- `Warning` - Advarsler
- `Error` - Feil

## 🤝 Bidra

Vi setter pris på bidrag! 

1. Fork prosjektet
2. Lag en feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit endringene (`git commit -m 'Add some AmazingFeature'`)
4. Push til branchen (`git push origin feature/AmazingFeature`)
5. Åpne en Pull Request

## 📄 Lisens

Dette prosjektet er lisensiert under MIT License - se [LICENSE](LICENSE) fil for detaljer.

## 👥 Forfattere

- **Orientering Bergen** - [o-bergen.no](https://o-bergen.no)

## 🙏 Acknowledgments

- EQTiming for timing-systemet
- Supabase for backend-infrastruktur
- .NET Community

## 📞 Support

For problemer eller spørsmål:
- Åpne en [Issue](https://github.com/o-bergen/LiveResultManager/issues)
- Kontakt: [kontakt@o-bergen.no](mailto:kontakt@o-bergen.no)

---

**Laget med ❤️ for orientering i Bergen**
