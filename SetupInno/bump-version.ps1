param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\DjApplication3.WinUI\DjApplication3.WinUI.csproj"),
    [string]$InstallerPath = (Join-Path $PSScriptRoot "DjApplication3.iss"),
    [switch]$WriteGithubOutput
)

$ErrorActionPreference = "Stop"

function Set-Utf8NoBomContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Value, $encoding)
}

function Add-GithubOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::AppendAllText($env:GITHUB_OUTPUT, "$Name=$Value$([System.Environment]::NewLine)", $encoding)
}

$projectFullPath = [System.IO.Path]::GetFullPath($ProjectPath)
$installerFullPath = [System.IO.Path]::GetFullPath($InstallerPath)

$projectContent = Get-Content -LiteralPath $projectFullPath -Raw
$versionMatch = [regex]::Match($projectContent, "<Version>(\d+)\.(\d+)\.(\d+)\.(\d+)</Version>")
if (-not $versionMatch.Success) {
    throw "Version introuvable dans '$projectFullPath'. Format attendu: 1.2.3.4"
}

$parts = @(
    [int]$versionMatch.Groups[1].Value,
    [int]$versionMatch.Groups[2].Value,
    [int]$versionMatch.Groups[3].Value,
    [int]$versionMatch.Groups[4].Value
)
$parts[3]++

$newVersion = $parts -join "."
$projectContent = [regex]::Replace($projectContent, "<Version>[^<]+</Version>", "<Version>$newVersion</Version>", 1)
$projectContent = [regex]::Replace($projectContent, "<FileVersion>[^<]+</FileVersion>", "<FileVersion>$newVersion</FileVersion>", 1)
Set-Utf8NoBomContent -Path $projectFullPath -Value $projectContent

$installerContent = Get-Content -LiteralPath $installerFullPath -Raw
if (-not [regex]::IsMatch($installerContent, '#define MyAppVersion "[^"]+"')) {
    throw "MyAppVersion introuvable dans '$installerFullPath'."
}

$installerContent = [regex]::Replace($installerContent, '#define MyAppVersion "[^"]+"', "#define MyAppVersion `"$newVersion`"", 1)
Set-Utf8NoBomContent -Path $installerFullPath -Value $installerContent

Write-Host "Version: $newVersion"

if ($WriteGithubOutput) {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        throw "GITHUB_OUTPUT est indisponible."
    }

    Add-GithubOutput -Name "version" -Value $newVersion
}
