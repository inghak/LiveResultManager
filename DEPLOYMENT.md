 # Live Result Manager - Deployment Guide

 ## 🚀 Quick Build

 ```powershell
 # Build (default: self-contained, single exe, ~85 MB)
 .\build-production.ps1

 # Output: publish\LiveResultManager.exe
 ```

 ### Build Options

 ```powershell
 # Self-contained (recommended) - no .NET required
 .\build-production.ps1 -BuildType self-contained

 # Framework-dependent - smaller (~8 MB), requires .NET 10 Runtime
 .\build-production.ps1 -BuildType framework-dependent

 # ReadyToRun - faster startup (~110 MB)
 .\build-production.ps1 -BuildType ready-to-run
 ```

 ## 📦 Deploy

 1. Copy `publish\LiveResultManager.exe` to target machine
 2. Copy `appsettings.json` and configure:
    - AccessDB path
    - Supabase URL and API key
    - Polling interval

 ## ⚙️ Configuration Example

 `appsettings.json`:
 ```json
 {
   "SourceType": "AccessDb",
   "AccessDbPath": "C:\\Path\\To\\Database.mdb",
   "DestinationType": "Supabase",
   "Supabase": {
     "Url": "https://your-project.supabase.co",
     "ApiKey": "your-api-key"
   },
   "PollingIntervalSeconds": 30
 }
 ```

 ## 🚀 Auto-Start (Optional)

 ### Task Scheduler (Windows)
 ```powershell
 .\setup-autostart.ps1 -Method TaskScheduler -ExePath "C:\Path\To\LiveResultManager.exe"
 ```

 ### Startup Folder (Simple)
 ```powershell
 .\setup-autostart.ps1 -Method StartupFolder -ExePath "C:\Path\To\LiveResultManager.exe"
 ```

 ## 📋 Requirements

 - **For AccessDB:** [Microsoft Access Database Engine 2016](https://www.microsoft.com/en-us/download/details.aspx?id=54920)
 - **For framework-dependent build:** [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

 ## 🐛 Troubleshooting

 | Problem | Solution |
 |---------|----------|
 | "Could not load file or assembly..." | Use self-contained build or install .NET 10 Runtime |
 | AccessDB connection fails | Install Access Database Engine 2016 (64-bit) |
 | Supabase connection fails | Check firewall, verify API credentials |

