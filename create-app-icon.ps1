#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a simple application icon for Live Result Manager
.DESCRIPTION
    Generates a basic icon using Windows built-in capabilities or downloads from Iconoir
#>

param(
    [string]$OutputPath = "o-bergen.LiveResultManager\Resources\app-icon.ico"
)

Write-Host "🎨 Creating icon for Live Result Manager..." -ForegroundColor Cyan

# Create Resources directory if it doesn't exist
$resourceDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $resourceDir)) {
    New-Item -ItemType Directory -Path $resourceDir -Force | Out-Null
    Write-Host "✓ Created directory: $resourceDir" -ForegroundColor Green
}

# Option 1: Try to download from Iconoir (open source, free)
$iconUrl = "https://raw.githubusercontent.com/iconoir-icons/iconoir/main/icons/timer.svg"
$tempSvg = "$env:TEMP\timer-icon.svg"

try {
    Write-Host "📥 Downloading icon from Iconoir (open source)..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $iconUrl -OutFile $tempSvg -UseBasicParsing
    Write-Host "✓ Icon downloaded" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠️  SVG downloaded to: $tempSvg" -ForegroundColor Yellow
    Write-Host "   You need to convert it to .ico format using one of these methods:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   Method 1: Online converter (easiest)" -ForegroundColor Cyan
    Write-Host "   1. Go to https://convertico.com/" -ForegroundColor White
    Write-Host "   2. Upload: $tempSvg" -ForegroundColor White
    Write-Host "   3. Select sizes: 256, 128, 64, 48, 32, 16" -ForegroundColor White
    Write-Host "   4. Download as app-icon.ico" -ForegroundColor White
    Write-Host "   5. Save to: $OutputPath" -ForegroundColor White
    Write-Host ""
    Write-Host "   Method 2: ImageMagick (if installed)" -ForegroundColor Cyan
    Write-Host "   Run: magick convert `"$tempSvg`" -define icon:auto-resize=256,128,64,48,32,16 `"$OutputPath`"" -ForegroundColor White
    Write-Host ""
    Write-Host "   Method 3: Use Paint.NET / GIMP" -ForegroundColor Cyan
    Write-Host "   1. Open $tempSvg" -ForegroundColor White
    Write-Host "   2. Resize to 256x256" -ForegroundColor White
    Write-Host "   3. Export as ICO with multiple sizes" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "❌ Could not download icon: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Create your own icon" -ForegroundColor Yellow
    Write-Host "1. Use Microsoft Designer: https://designer.microsoft.com/image-creator" -ForegroundColor White
    Write-Host "   Prompt: 'Simple stopwatch icon for sports timing app, flat design, blue and orange'" -ForegroundColor Gray
    Write-Host "2. Or download from: https://www.flaticon.com/search?word=stopwatch" -ForegroundColor White
    Write-Host "3. Convert to .ico: https://convertico.com/" -ForegroundColor White
    Write-Host "4. Save as: $OutputPath" -ForegroundColor White
}

Write-Host ""
Write-Host "After you have created app-icon.ico, run:" -ForegroundColor Cyan
Write-Host "  .\apply-app-icon.ps1" -ForegroundColor Green
Write-Host ""
Write-Host "This will update the project files to use the icon." -ForegroundColor Gray
