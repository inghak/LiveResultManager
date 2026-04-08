# Setup GitHub Repository
# Dette scriptet hjelper deg med å sette opp GitHub repository for Live Result Manager

param(
    [Parameter(Mandatory=$false)]
    [string]$RepoName = "LiveResultManager",
    
    [Parameter(Mandatory=$false)]
    [string]$GitHubUsername = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$InitOnly
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Live Result Manager - GitHub Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if git is installed
try {
    $gitVersion = git --version
    Write-Host "✓ Git er installert: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Git er ikke installert!" -ForegroundColor Red
    Write-Host "Last ned fra: https://git-scm.com/download/win" -ForegroundColor Yellow
    exit 1
}

# Check if already a git repository
if (Test-Path ".git") {
    Write-Host "✓ Git repository allerede initialisert" -ForegroundColor Green
    
    $currentRemote = git remote get-url origin 2>$null
    if ($currentRemote) {
        Write-Host "✓ Remote 'origin' er satt til: $currentRemote" -ForegroundColor Green
        Write-Host ""
        Write-Host "For å pushe til GitHub, kjør:" -ForegroundColor Cyan
        Write-Host "  git push -u origin main" -ForegroundColor White
        exit 0
    }
} else {
    Write-Host "Initialiserer Git repository..." -ForegroundColor Yellow
    
    # Initialize git
    git init
    
    # Set default branch to main
    git branch -M main
    
    Write-Host "✓ Git repository initialisert med 'main' branch" -ForegroundColor Green
}

# Add all files
Write-Host ""
Write-Host "Legger til filer..." -ForegroundColor Yellow
git add .

# Create initial commit
Write-Host "Oppretter initial commit..." -ForegroundColor Yellow
git commit -m "Initial commit: Live Result Manager v1.0

- EQTiming Access Database to Supabase sync
- Auto-polling and delta detection
- Clean Architecture with Repository Pattern
- JSON archiving
- Auto-start on Windows boot
- GUI for configuration"

Write-Host "✓ Initial commit opprettet" -ForegroundColor Green

if ($InitOnly) {
    Write-Host ""
    Write-Host "Git repository er klar!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Neste steg:" -ForegroundColor Cyan
    Write-Host "1. Opprett et nytt repository på GitHub:" -ForegroundColor White
    Write-Host "   https://github.com/new" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Koble til GitHub repository:" -ForegroundColor White
    Write-Host "   git remote add origin https://github.com/BRUKERNAVN/$RepoName.git" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Push koden:" -ForegroundColor White
    Write-Host "   git push -u origin main" -ForegroundColor White
    exit 0
}

# Prompt for GitHub username if not provided
if ([string]::IsNullOrWhiteSpace($GitHubUsername)) {
    Write-Host ""
    $GitHubUsername = Read-Host "Skriv inn ditt GitHub brukernavn (eller organisasjon)"
}

if ([string]::IsNullOrWhiteSpace($GitHubUsername)) {
    Write-Host "✗ GitHub brukernavn er påkrevd" -ForegroundColor Red
    exit 1
}

# Set remote
$remoteUrl = "https://github.com/$GitHubUsername/$RepoName.git"
Write-Host ""
Write-Host "Setter remote URL til: $remoteUrl" -ForegroundColor Yellow

try {
    git remote add origin $remoteUrl
    Write-Host "✓ Remote 'origin' lagt til" -ForegroundColor Green
} catch {
    Write-Host "Remote 'origin' eksisterer allerede, oppdaterer..." -ForegroundColor Yellow
    git remote set-url origin $remoteUrl
    Write-Host "✓ Remote 'origin' oppdatert" -ForegroundColor Green
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Setup fullført!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Neste steg:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Gå til GitHub og opprett et nytt repository:" -ForegroundColor White
Write-Host "   https://github.com/new" -ForegroundColor White
Write-Host "   - Repository navn: $RepoName" -ForegroundColor Gray
Write-Host "   - Beskrivelse: Automatic transfer of orienteering results from EQTiming to Supabase" -ForegroundColor Gray
Write-Host "   - Public eller Private (ditt valg)" -ForegroundColor Gray
Write-Host "   - IKKE initialiser med README eller gitignore eller license (vi har dem allerede)" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Push koden til GitHub:" -ForegroundColor White
Write-Host "   git push -u origin main" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Hvis du får autentiseringsfeil, opprett Personal Access Token:" -ForegroundColor White
Write-Host "   https://github.com/settings/tokens" -ForegroundColor White
Write-Host "   - Velg repo scope og bruk token som passord" -ForegroundColor Gray
Write-Host ""
