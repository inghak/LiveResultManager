# Quick Setup - Run as Administrator
# This will install LiveResultManager to run automatically on Windows startup

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Live Result Manager - Quick Auto-Start" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Please run PowerShell as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

# Default installation path
$defaultPath = "C:\LiveResultManager"
$publishPath = ".\publish\LiveResultManager.exe"

Write-Host "Where would you like to install LiveResultManager?" -ForegroundColor Yellow
Write-Host "Default: $defaultPath" -ForegroundColor Gray
Write-Host ""
$installPath = Read-Host "Installation path (press Enter for default)"

if ([string]::IsNullOrWhiteSpace($installPath)) {
    $installPath = $defaultPath
}

# Create installation directory
if (-not (Test-Path $installPath)) {
    Write-Host "Creating installation directory..." -ForegroundColor Green
    New-Item -Path $installPath -ItemType Directory -Force | Out-Null
}

# Check if we have a published build
if (-not (Test-Path $publishPath)) {
    Write-Host ""
    Write-Host "ERROR: No published build found!" -ForegroundColor Red
    Write-Host "Please run the build script first:" -ForegroundColor Yellow
    Write-Host "  .\build-production.ps1" -ForegroundColor White
    Write-Host ""
    pause
    exit 1
}

# Copy files
Write-Host ""
Write-Host "Installing files to $installPath..." -ForegroundColor Green

Copy-Item -Path ".\publish\*" -Destination $installPath -Recurse -Force

$exePath = Join-Path $installPath "LiveResultManager.exe"
$configPath = Join-Path $installPath "appsettings.json"

Write-Host "Installation complete!" -ForegroundColor Green
Write-Host ""

# Configure appsettings
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Do you want to configure settings now? (Y/N)" -ForegroundColor Yellow
$configure = Read-Host

if ($configure -eq 'Y' -or $configure -eq 'y') {
    Write-Host ""
    Write-Host "Opening configuration file..." -ForegroundColor Green
    Write-Host "Edit the file and save it, then close Notepad to continue." -ForegroundColor Yellow
    notepad $configPath
    Write-Host "Configuration saved!" -ForegroundColor Green
}

# Setup auto-start
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Auto-Start Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Setup automatic startup using Task Scheduler?" -ForegroundColor Yellow
Write-Host "This will start LiveResultManager automatically when Windows boots." -ForegroundColor Gray
Write-Host ""
$setupAuto = Read-Host "Setup auto-start? (Y/N)"

if ($setupAuto -eq 'Y' -or $setupAuto -eq 'y') {
    Write-Host ""
    Write-Host "Setting up Task Scheduler..." -ForegroundColor Green
    
    # Create scheduled task
    $action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory $installPath
    $trigger = New-ScheduledTaskTrigger -AtStartup
    $trigger.Delay = "PT2M"
    
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -RestartCount 3 `
        -RestartInterval (New-TimeSpan -Minutes 1)
    
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
    
    Register-ScheduledTask `
        -TaskName "LiveResultManager" `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -Principal $principal `
        -Description "Automatic transfer of orienteering race results" `
        -Force | Out-Null
    
    Write-Host ""
    Write-Host "Auto-start configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "The application will start automatically:" -ForegroundColor Cyan
    Write-Host "  - 2 minutes after Windows boots" -ForegroundColor White
    Write-Host "  - Even if no user is logged in" -ForegroundColor White
    Write-Host "  - Will auto-restart on failure" -ForegroundColor White
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installation path: $installPath" -ForegroundColor White
Write-Host "Executable: $exePath" -ForegroundColor White
Write-Host "Configuration: $configPath" -ForegroundColor White
Write-Host ""

if ($setupAuto -eq 'Y' -or $setupAuto -eq 'y') {
    Write-Host "Auto-start: Enabled (Task Scheduler)" -ForegroundColor Green
    Write-Host ""
    Write-Host "To test now:" -ForegroundColor Yellow
    Write-Host "  1. Open Task Scheduler" -ForegroundColor White
    Write-Host "  2. Find 'LiveResultManager' task" -ForegroundColor White
    Write-Host "  3. Right-click -> Run" -ForegroundColor White
} else {
    Write-Host "Auto-start: Not configured" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To enable auto-start later, run:" -ForegroundColor Yellow
    Write-Host "  .\setup-autostart.ps1 -Method TaskScheduler -ExePath '$exePath'" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To start manually now:" -ForegroundColor Yellow
Write-Host "  & '$exePath'" -ForegroundColor Gray
Write-Host ""
Write-Host "To uninstall, run:" -ForegroundColor Yellow
Write-Host "  .\uninstall.ps1" -ForegroundColor Gray
Write-Host ""

# Create uninstall script
$uninstallScript = @"
# Uninstall LiveResultManager

`$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not `$isAdmin) {
    Write-Host "ERROR: Please run as Administrator!" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "Uninstalling LiveResultManager..." -ForegroundColor Yellow

# Remove scheduled task
Unregister-ScheduledTask -TaskName "LiveResultManager" -Confirm:`$false -ErrorAction SilentlyContinue

# Remove installation directory
Remove-Item -Path "$installPath" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "LiveResultManager has been uninstalled." -ForegroundColor Green
pause
"@

Set-Content -Path (Join-Path $installPath "uninstall.ps1") -Value $uninstallScript

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
