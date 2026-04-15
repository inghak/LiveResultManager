param(
    [string]$IconPath = 'o-bergen.LiveResultManager\Resources\app-icon.ico'
)

Write-Host '🎨 Applying icon to Live Result Manager...' -ForegroundColor Cyan

if (-not (Test-Path $IconPath)) {
    Write-Host '❌ Icon file not found' -ForegroundColor Red
    Write-Host "   Expected at: $IconPath" -ForegroundColor Yellow
    Write-Host ''
    Write-Host 'Please create the icon file first or manually place your icon at the path above' -ForegroundColor Yellow
    exit 1
}

Write-Host '✓ Found icon file' -ForegroundColor Green

$relativePath = 'Resources\app-icon.ico'
Write-Host '📝 Updating project file...' -ForegroundColor Yellow

$csprojPath = 'o-bergen.LiveResultManager\o-bergen.LiveResultManager.csproj'
$csprojContent = Get-Content $csprojPath -Raw

if ($csprojContent -match '<ApplicationIcon>') {
    Write-Host '⚠️  Updating existing ApplicationIcon' -ForegroundColor Yellow
    $csprojContent = $csprojContent -replace '<ApplicationIcon>.*?</ApplicationIcon>', "<ApplicationIcon>$relativePath</ApplicationIcon>"
} else {
    $csprojContent = $csprojContent -replace '(</PropertyGroup>)', "    <ApplicationIcon>$relativePath</ApplicationIcon>`r`n  `$1"
}

Set-Content -Path $csprojPath -Value $csprojContent -NoNewline
Write-Host '✓ Project file updated' -ForegroundColor Green

Write-Host ''
Write-Host '✅ Icon configuration complete!' -ForegroundColor Green
Write-Host ''
Write-Host 'The icon will be used for:' -ForegroundColor Cyan
Write-Host '  • Application executable' -ForegroundColor White
Write-Host '  • Taskbar icon' -ForegroundColor White
Write-Host '  • Window title bar' -ForegroundColor White
Write-Host ''
Write-Host 'Next steps:' -ForegroundColor Yellow
Write-Host '  1. Rebuild the project' -ForegroundColor White
Write-Host '  2. Run publish-release.ps1' -ForegroundColor White
Write-Host ''
