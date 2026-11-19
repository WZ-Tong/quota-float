[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$CompilerPath = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
$utf8 = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = $utf8
try { [Console]::OutputEncoding = $utf8 } catch { }
$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }

function Find-Csc {
    param([string]$Explicit)

    if ($Explicit) {
        if (Test-Path -LiteralPath $Explicit) {
            return (Resolve-Path -LiteralPath $Explicit).Path
        }
        throw "CompilerPath not found: $Explicit"
    }

    if ($env:CSC) {
        if (Test-Path -LiteralPath $env:CSC) {
            return $env:CSC
        }
        throw "CSC points to a missing file: $($env:CSC)"
    }

    $command = Get-Command csc.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $windows = $env:WINDIR
    if (-not $windows) {
        $windows = [Environment]::GetFolderPath("Windows")
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($arch in @("Framework64", "Framework")) {
        $base = Join-Path $windows ("Microsoft.NET\" + $arch)
        if (-not (Test-Path -LiteralPath $base)) { continue }
        $dirs = Get-ChildItem -LiteralPath $base -Directory -Filter "v4.*" -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending
        foreach ($dir in $dirs) {
            $csc = Join-Path $dir.FullName "csc.exe"
            if (Test-Path -LiteralPath $csc) { [void]$candidates.Add($csc) }
        }
    }

    if ($candidates.Count -gt 0) { return $candidates[0] }
    throw "Could not find a .NET Framework 4.x csc.exe. Enable .NET Framework 4.x, or set -CompilerPath / CSC."
}

if ([string]::IsNullOrWhiteSpace($Output)) {
    if ($Configuration -eq "Debug") { $Output = "QuotaFloat.debug.exe" } else { $Output = "QuotaFloat.exe" }
}

$csc = Find-Csc $CompilerPath
$manifest = Join-Path $root "app.manifest"
$icon = Join-Path $root "app.ico"
if (-not (Test-Path -LiteralPath $manifest)) { throw "Missing app.manifest: $manifest" }
if (-not (Test-Path -LiteralPath $icon)) { throw "Missing app.ico: $icon" }

$allSources = @(
    Get-ChildItem -LiteralPath $root -Filter "*.cs" -File -Recurse |
        Where-Object { $_.FullName -notmatch "[\\/](bin|obj)[\\/]" } |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
)
if ($Configuration -eq "Debug") {
    $sources = $allSources
} else {
    $sources = @($allSources | Where-Object {
        $name = Split-Path -Leaf $_
        $name -ne "DebugShell.cs" -and $name -ne "MainForm.Debug.cs"
    })
}
if ($sources.Count -eq 0) { throw "No .cs source files found." }

if ([System.IO.Path]::IsPathRooted($Output)) {
    $outPath = $Output
} else {
    $outPath = Join-Path $root $Output
}

$common = @(
    "-nologo",
    "-codepage:65001",
    "-utf8output",
    "-win32manifest:$manifest",
    "-win32icon:$icon",
    "-out:$outPath",
    "-reference:System.dll",
    "-reference:System.Core.dll",
    "-reference:System.Drawing.dll",
    "-reference:System.Windows.Forms.dll",
    "-reference:System.Web.Extensions.dll",
    "-reference:System.Security.dll"
)

if ($Configuration -eq "Debug") {
    $cscArgs = @("-target:exe", "-optimize-", "-debug:full", "-define:DEBUG") + $common + $sources
} else {
    $cscArgs = @("-target:winexe", "-optimize+", "-debug-") + $common + $sources
}

Write-Host "Compiler     : $csc"
Write-Host "Configuration: $Configuration"
Write-Host "Sources      : $($sources.Count)"

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. If QuotaFloat.exe is running, close it and retry." -ForegroundColor Yellow
    exit $LASTEXITCODE
}

$file = Get-Item -LiteralPath $outPath
Write-Host ("Output       : {0} ({1:N0} bytes)" -f $file.FullName, $file.Length)
$pdb = [System.IO.Path]::ChangeExtension($outPath, ".pdb")
if (Test-Path -LiteralPath $pdb) {
    $pdbFile = Get-Item -LiteralPath $pdb
    Write-Host ("Symbols      : {0} ({1:N0} bytes)" -f $pdbFile.FullName, $pdbFile.Length)
}
exit 0
