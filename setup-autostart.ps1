# Live Result Manager - Auto-Start Setup Script
# Run as Administrator

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('TaskScheduler', 'StartupFolder', 'Registry')]
    [string]$Method,
    
    [Parameter(Mandatory=$true)]
    [string]$ExePath,
    
    [string]$TaskName = "LiveResultManager",
    
    [int]$StartupDelayMinutes = 2,
    
    [switch]$Remove
)

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin -and $Method -in @('TaskScheduler', 'Registry')) {
    Write-Host "ERROR: This method requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

# Verify exe exists
if (-not $Remove -and -not (Test-Path $ExePath)) {
    Write-Host "ERROR: Executable not found at: $ExePath" -ForegroundColor Red
    exit 1
}

$exeDir = Split-Path -Parent $ExePath

Write-Host "Live Result Manager Auto-Start Setup" -ForegroundColor Cyan
Write-Host "Method: $Method" -ForegroundColor Yellow
Write-Host "Executable: $ExePath" -ForegroundColor Yellow
Write-Host ""

switch ($Method) {
    'TaskScheduler' {
        if ($Remove) {
            Write-Host "Removing scheduled task..." -ForegroundColor Yellow
            Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
            Write-Host "Task removed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Creating scheduled task..." -ForegroundColor Green
            
            # Create action
            $action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $exeDir
            
            # Create trigger (at startup with delay)
            $trigger = New-ScheduledTaskTrigger -AtStartup
            $trigger.Delay = "PT$($StartupDelayMinutes)M"
            
            # Create settings
            $settings = New-ScheduledTaskSettingsSet `
                -AllowStartIfOnBatteries `
                -DontStopIfGoingOnBatteries `
                -StartWhenAvailable `
                -RestartCount 3 `
                -RestartInterval (New-TimeSpan -Minutes 1)
            
            # Create principal (run with highest privileges)
            $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
            
            # Register task
            Register-ScheduledTask `
                -TaskName $TaskName `
                -Action $action `
                -Trigger $trigger `
                -Settings $settings `
                -Principal $principal `
                -Description "Automatic transfer of orienteering race results to live platforms" `
                -Force
            
            Write-Host ""
            Write-Host "Task Scheduler setup complete!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Task details:" -ForegroundColor Cyan
            Write-Host "  Name: $TaskName" -ForegroundColor White
            Write-Host "  Trigger: At system startup (delay: $StartupDelayMinutes minutes)" -ForegroundColor White
            Write-Host "  Run as: SYSTEM account" -ForegroundColor White
            Write-Host "  Auto-restart on failure: Yes (3 attempts)" -ForegroundColor White
            Write-Host ""
            Write-Host "To test: Right-click task in Task Scheduler and select 'Run'" -ForegroundColor Yellow
            Write-Host "To view: Open Task Scheduler and look for '$TaskName'" -ForegroundColor Yellow
        }
    }
    
    'StartupFolder' {
        $startupFolder = [Environment]::GetFolderPath('Startup')
        $shortcutPath = Join-Path $startupFolder "$TaskName.lnk"
        
        if ($Remove) {
            Write-Host "Removing startup shortcut..." -ForegroundColor Yellow
            Remove-Item -Path $shortcutPath -ErrorAction SilentlyContinue
            Write-Host "Shortcut removed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Creating startup folder shortcut..." -ForegroundColor Green
            
            $WshShell = New-Object -ComObject WScript.Shell
            $Shortcut = $WshShell.CreateShortcut($shortcutPath)
            $Shortcut.TargetPath = $ExePath
            $Shortcut.WorkingDirectory = $exeDir
            $Shortcut.Description = "Live Result Manager - Auto-start"
            $Shortcut.Save()
            
            Write-Host ""
            Write-Host "Startup folder setup complete!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Shortcut location: $shortcutPath" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "NOTE: This will only start when user logs in!" -ForegroundColor Yellow
            Write-Host "For always-on operation, use Task Scheduler instead." -ForegroundColor Yellow
        }
    }
    
    'Registry' {
        $regPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
        
        if ($Remove) {
            Write-Host "Removing registry entry..." -ForegroundColor Yellow
            Remove-ItemProperty -Path $regPath -Name $TaskName -ErrorAction SilentlyContinue
            Write-Host "Registry entry removed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Creating registry run key..." -ForegroundColor Green
            Write-Host "WARNING: Task Scheduler is preferred over Registry method!" -ForegroundColor Yellow
            
            New-ItemProperty -Path $regPath `
                -Name $TaskName `
                -Value "`"$ExePath`"" `
                -PropertyType String `
                -Force | Out-Null
            
            Write-Host ""
            Write-Host "Registry setup complete!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Registry key: $regPath\$TaskName" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "NOTE: This method is not recommended for production!" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "Setup completed successfully!" -ForegroundColor Green

if (-not $Remove) {
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Ensure appsettings.json is configured correctly" -ForegroundColor White
    Write-Host "2. Test the application manually first" -ForegroundColor White
    Write-Host "3. Restart computer to verify auto-start" -ForegroundColor White
    Write-Host ""
    Write-Host "To remove auto-start, run:" -ForegroundColor Yellow
    Write-Host "  .\setup-autostart.ps1 -Method $Method -ExePath '$ExePath' -Remove" -ForegroundColor Gray
}
