[CmdletBinding()]
param(
    [string]$Tag = "",
    [switch]$CheckRemote
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
$build = Join-Path $root "build.ps1"
$single = Join-Path $root "QuotaFloat.single.cs"
$assemblyInfo = Join-Path $root "AssemblyInfo.cs"

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $build -ExportOnly
if ($LASTEXITCODE -ne 0) { throw "Single-source export failed." }

$changed = @(git -C $root status --porcelain -- "QuotaFloat.single.cs")
if ($changed.Count -gt 0) {
    throw "QuotaFloat.single.cs is not up to date: $($changed -join ', ')"
}

$text = Get-Content -LiteralPath $assemblyInfo -Raw
$assemblyVersion = [regex]::Match($text, 'AssemblyVersion\("([^"]+)"\)').Groups[1].Value
$fileVersion = [regex]::Match($text, 'AssemblyFileVersion\("([^"]+)"\)').Groups[1].Value
$informational = [regex]::Match($text, 'AssemblyInformationalVersion\("([^"]+)"\)').Groups[1].Value
if (-not $assemblyVersion -or -not $fileVersion -or -not $informational) {
    throw "AssemblyInfo.cs is missing version metadata."
}

Write-Host "Assembly     : $assemblyVersion"
Write-Host "File version : $fileVersion"
Write-Host "Informational: $informational"

if ($Tag) {
    $tagVersion = $Tag.TrimStart("v")
    if ($informational -ne $tagVersion) {
        throw "Tag $Tag does not match InformationalVersion $informational."
    }
}

$releaseExe = Join-Path $root "QuotaFloat.exe"
if (Test-Path -LiteralPath $releaseExe) {
    $info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($releaseExe)
    if ($info.FileVersion -ne $fileVersion) {
        throw "QuotaFloat.exe FileVersion $($info.FileVersion) does not match $fileVersion."
    }
}

if ($CheckRemote) {
    if (-not $Tag) { throw "-CheckRemote requires -Tag." }
    $names = @(& gh release view $Tag --json assets --jq '.assets[].name' 2>$null)
    if ($LASTEXITCODE -ne 0) { throw "Could not read GitHub release $Tag." }
    if ($names.Count -ne 1 -or $names[0] -ne "QuotaFloat.exe") {
        throw "Release $Tag assets must contain only QuotaFloat.exe; found: $($names -join ', ')"
    }
}

Write-Host "VERIFY PASS"
exit 0
