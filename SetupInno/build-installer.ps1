$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "DjApplication3.WinUI\DjApplication3.WinUI.csproj"
$issPath = Join-Path $PSScriptRoot "DjApplication3.iss"
$publishPath = Join-Path $repoRoot "DjApplication3.WinUI\bin\Release\net8.0-windows10.0.19041.0\publish"

Get-Process -Name "DjApplication3.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force

dotnet publish $projectPath -c Release --no-restore

$requiredFiles = @(
    "App.xbf",
    "MainWindow.xbf",
    "DjApplication3.WinUI.pri",
    "Views\MainView.xbf",
    "Controls\DeckControl.xbf",
    "Controls\TrackBarPerso.xbf",
    "Controls\WaveformControl.xbf"
)

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $publishPath $file
    if (-not (Test-Path $fullPath)) {
        throw "Fichier WinUI manquant dans le publish: $file"
    }
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "Inno Setup 6 est introuvable. Installe Inno Setup, puis relance ce script."
}

& $iscc $issPath
