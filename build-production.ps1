# Live Result Manager - Production Build Script

param(
    [ValidateSet('self-contained', 'framework-dependent', 'ready-to-run')]
    [string]$BuildType = 'self-contained',
    [string]$OutputPath = './publish',
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)

Write-Host "Building Live Result Manager for Production" -ForegroundColor Cyan
Write-Host "Build Type: $BuildType" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Output: $OutputPath" -ForegroundColor Yellow
Write-Host ""

if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Gray
    Remove-Item -Path $OutputPath -Recurse -Force
}

$projectPath = "o-bergen.LiveResultManager/o-bergen.LiveResultManager.csproj"

switch ($BuildType) {
    'self-contained' {
        Write-Host "Building self-contained executable..." -ForegroundColor Green
        dotnet publish $projectPath -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputPath
    }
    'framework-dependent' {
        Write-Host "Building framework-dependent executable..." -ForegroundColor Green
        dotnet publish $projectPath -c Release -r $Runtime --self-contained false -p:PublishSingleFile=true -o $OutputPath
    }
    'ready-to-run' {
        Write-Host "Building with ReadyToRun..." -ForegroundColor Green
        dotnet publish $projectPath -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputPath
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output location: $OutputPath" -ForegroundColor Cyan
    Write-Host ""
    Get-ChildItem -Path $OutputPath | ForEach-Object {
        $size = if ($_.Length -gt 1MB) { "{0:N2} MB" -f ($_.Length / 1MB) } else { "{0:N2} KB" -f ($_.Length / 1KB) }
        Write-Host "  - $($_.Name) ($size)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Main executable: $OutputPath\LiveResultManager.exe" -ForegroundColor Yellow
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
