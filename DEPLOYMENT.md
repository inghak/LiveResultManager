# Live Result Manager - Deployment Guide

## 🚀 Building for Production

### Quick Start

```powershell
# Build self-contained exe (recommended)
.\publish-release.ps1

# Or specify build type
.\publish-release.ps1 -BuildType self-contained
.\publish-release.ps1 -BuildType framework-dependent
.\publish-release.ps1 -BuildType ready-to-run
```

### Build Types

#### 1. Self-Contained (Anbefalt)
- **Fordeler:** Inkluderer .NET runtime, fungerer uten installasjon
- **Størrelse:** ~80-100 MB
- **Bruk:** `.\publish-release.ps1 -BuildType self-contained`

#### 2. Framework-Dependent
- **Fordeler:** Mindre filstørrelse (~5-10 MB)
- **Ulemper:** Krever .NET 10 Runtime installert
- **Bruk:** `.\publish-release.ps1 -BuildType framework-dependent`

#### 3. ReadyToRun
- **Fordeler:** Raskere oppstart, self-contained
- **Størrelse:** ~100-120 MB
- **Bruk:** `.\publish-release.ps1 -BuildType ready-to-run`

### Manual Build Commands

```powershell
# Self-contained single file
dotnet publish o-bergen.LiveResultManager/o-bergen.LiveResultManager.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish

# Framework-dependent
dotnet publish o-bergen.LiveResultManager/o-bergen.LiveResultManager.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -o ./publish
```

## 📦 Distribution Package

### Minimale filer som trengs:

```
publish/
├── LiveResultManager.exe          # Hovedapplikasjon
├── appsettings.json               # Konfigurasjon (må tilpasses!)
└── appsettings.Production.json    # (Valgfri) Prod-spesifikk config
```

### For AccessDB Source:
Brukere må ha **Microsoft Access Database Engine** installert:
- [Download 64-bit](https://www.microsoft.com/en-us/download/details.aspx?id=54920)
- Eller inkluder instruksjoner i README

## ⚙️ Production Configuration

### 1. Opprett Production Config

Lag `appsettings.Production.json`:

```json
{
  "SourceType": "AccessDb",
  "AccessDbPath": "C:\\Path\\To\\Production\\Database.mdb",
  "DestinationType": "Supabase",
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ApiKey": "your-production-api-key"
  },
  "PollingIntervalSeconds": 30,
  "EnableArchive": true,
  "ArchivePath": "C:\\LiveResults\\Archive"
}
```

### 2. Bruk Environment Variables (Sikrest)

For sensitive data, bruk miljøvariabler:

```powershell
# Set production credentials
$env:Supabase__Url = "https://your-project.supabase.co"
$env:Supabase__ApiKey = "your-secure-api-key"
```

Eller opprett en `start-production.ps1`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Supabase__Url = "https://your-project.supabase.co"
$env:Supabase__ApiKey = "your-secure-api-key"

.\LiveResultManager.exe
```

## 🧪 Testing the Build

```powershell
# Test build locally
.\publish\LiveResultManager.exe

# Test on clean VM (recommended)
# - Fresh Windows installation
# - No Visual Studio or .NET SDK
# - Only .NET Runtime (if framework-dependent)
```

## 🚀 Auto-Start on Windows 11 Boot

### Method 1: Task Scheduler (Anbefalt for Production)

**Fordeler:**
- ✅ Starter selv om ingen er logget inn
- ✅ Kan kjøre med administrator-rettigheter
- ✅ Logger og feilhåndtering
- ✅ Delay ved oppstart (venter på nettverk)

**Setup med script:**

```powershell
# Kjør som Administrator
.\setup-autostart.ps1 -Method TaskScheduler -ExePath "C:\Path\To\LiveResultManager.exe"
```

**Manuell setup:**

1. Åpne **Task Scheduler** (Oppgaveplanlegging)
2. Klikk **Create Task** (ikke "Create Basic Task")
3. **General tab:**
   - Name: `LiveResultManager`
   - Description: `Automatic transfer of orienteering results`
   - ☑ Run whether user is logged on or not
   - ☑ Run with highest privileges
   - Configure for: Windows 11
4. **Triggers tab:**
   - New → Begin the task: **At startup**
   - Delay task for: **2 minutes** (la nettverk starte først)
   - ☑ Enabled
5. **Actions tab:**
   - New → Action: **Start a program**
   - Program/script: `C:\Path\To\LiveResultManager.exe`
   - Start in: `C:\Path\To\` (mappen der exe ligger)
6. **Conditions tab:**
   - ☑ Start only if the following network connection is available: **Any connection**
   - ☐ Uncheck "Stop if computer switches to battery power"
7. **Settings tab:**
   - ☑ Allow task to be run on demand
   - ☑ Run task as soon as possible after scheduled start is missed
   - If task fails, restart every: **1 minute**, up to **3 times**

### Method 2: Windows Startup Folder (Enklest)

**Fordeler:**
- ✅ Veldig enkelt å sette opp
- ✅ Synlig for brukeren

**Ulemper:**
- ❌ Krever at bruker logger inn
- ❌ Starter ikke som Administrator

**Setup:**

```powershell
# Kjør dette scriptet
.\setup-autostart.ps1 -Method StartupFolder -ExePath "C:\Path\To\LiveResultManager.exe"
```

**Manuelt:**

1. Trykk `Win + R`
2. Skriv: `shell:startup` og trykk Enter
3. Opprett en snarvei til `LiveResultManager.exe` i denne mappen

### Method 3: Windows Service (Mest Robust)

**Fordeler:**
- ✅ Kjører helt uavhengig av bruker-innlogging
- ✅ Automatisk restart ved feil
- ✅ Professional løsning

**Ulemper:**
- ❌ Krever kodeendringer (Windows Forms → headless service)
- ❌ Mer kompleks setup

**Dette krever konvertering til Windows Service** - se seksjonen under.

### Method 4: Registry Run Key (Ikke anbefalt)

For fullstendighet:

```powershell
# Legg til i registry (krever Administrator)
New-ItemProperty -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run" `
    -Name "LiveResultManager" `
    -Value "C:\Path\To\LiveResultManager.exe" `
    -PropertyType String -Force
```

**Merk:** Anbefales ikke for production - bruk Task Scheduler i stedet.

## 🛠️ Setup Scripts

## 🛠️ Setup Scripts

Tre scripts er inkludert for enkel installasjon:

### Quick Installation (Anbefalt)

```powershell
# 1. Bygg først
.\build-production.ps1

# 2. Installer og sett opp auto-start (kjør som Administrator)
.\install.ps1
```

Dette vil:
- ✅ Kopiere filer til `C:\LiveResultManager`
- ✅ La deg konfigurere settings
- ✅ Sette opp automatisk oppstart
- ✅ Opprette uninstall script

### Manual Auto-Start Setup

```powershell
# Task Scheduler (anbefalt)
.\setup-autostart.ps1 -Method TaskScheduler -ExePath "C:\Path\To\LiveResultManager.exe"

# Startup Folder (enklest)
.\setup-autostart.ps1 -Method StartupFolder -ExePath "C:\Path\To\LiveResultManager.exe"

# Remove auto-start
.\setup-autostart.ps1 -Method TaskScheduler -ExePath "C:\Path\To\LiveResultManager.exe" -Remove
```

## 📋 Deployment Checklist (Oppdatert)

**Development Phase:**
- [ ] Test applikasjon lokalt med development settings
- [ ] Verifiser AccessDB connection fungerer
- [ ] Verifiser Supabase connection fungerer
- [ ] Test metadata upload
- [ ] Test result transfer

**Build Phase:**
- [ ] Kjør `.\build-production.ps1`
- [ ] Verifiser at build var vellykket
- [ ] Test executable lokalt: `.\publish\LiveResultManager.exe`

**Production Configuration:**
- [ ] Oppdater `appsettings.json` med production-verdier
- [ ] Fjern/kommentér ut development credentials
- [ ] Sett riktig AccessDB path
- [ ] Sett riktig Supabase credentials
- [ ] Konfigurer polling interval (anbefalt: 30-60 sekunder)
- [ ] Konfigurer archive settings hvis ønsket

**Installation:**
- [ ] Kopier filer til prod-maskin
- [ ] Eller kjør `.\install.ps1` på prod-maskin (som Administrator)
- [ ] Verifiser at alle filer er kopiert
- [ ] Test manuell kjøring først

**Auto-Start Setup:**
- [ ] Kjør `.\setup-autostart.ps1` eller bruk `install.ps1`
- [ ] Verifiser at scheduled task er opprettet
- [ ] Test task manuelt fra Task Scheduler
- [ ] Restart maskinen og verifiser at app starter

**Final Testing:**
- [ ] Verifiser at app starter automatisk etter reboot
- [ ] Sjekk at results blir overført
- [ ] Verifiser logging fungerer
- [ ] Test error handling (f.eks. uten internett)
- [ ] Sjekk at arkivering fungerer (hvis aktivert)

**Documentation:**
- [ ] Dokumentér installasjonslokasjon
- [ ] Dokumentér login credentials for prod-maskin
- [ ] Dokumentér Supabase project info
- [ ] Lag support kontaktinfo

## 🎁 Lag Installer (Valgfritt)

### Bruk Inno Setup

1. Last ned [Inno Setup](https://jrsoftware.org/isinfo.php)
2. Lag `installer.iss` script
3. Inkluder:
   - Executable
   - Config templates
   - Shortcut på Desktop
   - Start menu entry

### Eller bruk WiX Toolset

For mer avanserte installasjoner med Windows Installer (.msi)

## 🔄 Automatisk Deployment

### GitHub Actions Example

```yaml
name: Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Publish
        run: |
          dotnet publish o-bergen.LiveResultManager/o-bergen.LiveResultManager.csproj `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -o ./release
      
      - name: Upload Release
        uses: actions/upload-artifact@v3
        with:
          name: LiveResultManager-win-x64
          path: ./release/LiveResultManager.exe
```

## 📊 Build Size Comparison

| Build Type | Size | Runtime Needed | Startup Time |
|------------|------|----------------|--------------|
| Self-Contained | ~85 MB | ❌ No | Fast |
| Ready-to-Run | ~110 MB | ❌ No | Fastest |
| Framework-Dependent | ~8 MB | ✅ .NET 10 | Fast |

## 🐛 Troubleshooting

### "Could not load file or assembly..."
- Bruk `--self-contained true` i build
- Eller installer .NET 10 Runtime

### AccessDB Connection Fails
- Installer Microsoft Access Database Engine 2016 Redistributable
- Match 32/64-bit version med executable

### Supabase Connection Fails
- Sjekk firewall settings
- Verifiser API credentials
- Test med curl/Postman først

## 📞 Support

For problemer, sjekk:
1. Build logs i publish-release.ps1
2. Runtime logs (hvis app krasjer)
3. Windows Event Viewer for detaljer
