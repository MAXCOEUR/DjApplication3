$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "DjApplication3.WinUI\DjApplication3.WinUI.csproj"
$issPath = Join-Path $PSScriptRoot "DjApplication3.iss"
$publishPath = Join-Path $repoRoot "DjApplication3.WinUI\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$repoFullPath = [System.IO.Path]::GetFullPath($repoRoot)
$publishFullPath = [System.IO.Path]::GetFullPath($publishPath)

Get-Process -Name "DjApplication3.WinUI" -ErrorAction SilentlyContinue | Stop-Process -Force

if ($publishFullPath.StartsWith($repoFullPath, [System.StringComparison]::OrdinalIgnoreCase) -and (Split-Path $publishFullPath -Leaf) -eq "publish") {
    Remove-Item -LiteralPath $publishFullPath -Recurse -Force -ErrorAction SilentlyContinue
}
else {
    throw "Chemin publish refuse par securite: $publishFullPath"
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish a echoue."
}

$requiredFiles = @(
    "DjApplication3.WinUI.exe",
    "DjApplication3.WinUI.dll",
    "DjApplication3.WinUI.pri",
    "App.xbf",
    "MainWindow.xbf",
    "Views\MainView.xbf",
    "Controls\DeckControl.xbf",
    "Controls\TrackBarPerso.xbf",
    "Controls\WaveformControl.xbf",
    "WebView2Loader.dll"
)

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $publishFullPath $file
    if (-not (Test-Path $fullPath)) {
        throw "Publish incomplet: fichier manquant '$file'"
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
