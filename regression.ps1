$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$build = Join-Path $root "build.ps1"
$exe = Join-Path $env:TEMP "QuotaFloat.regression.exe"
$pdb = [System.IO.Path]::ChangeExtension($exe, ".pdb")

try {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $build -Configuration Debug -Output $exe
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.Arguments = "--demo --dark --narrow"
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true

    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.StandardInput.AutoFlush = $true

    Start-Sleep -Milliseconds 1500
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("dock right")
    Start-Sleep -Milliseconds 300
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("hide")
    Start-Sleep -Milliseconds 500
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("show")
    Start-Sleep -Milliseconds 500
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("lang en")
    Start-Sleep -Milliseconds 300
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("lang zh-CN")
    Start-Sleep -Milliseconds 300
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("quit")

    if (-not $proc.WaitForExit(5000)) {
        $proc.Kill()
        throw "Process did not exit."
    }

    $out = $proc.StandardOutput.ReadToEnd()
    $err = $proc.StandardError.ReadToEnd()
    $lines = @($out -split "`r?`n" | Where-Object { $_ -like "status *" })

    if ($lines.Count -lt 6) {
        Write-Host $out
        Write-Host $err
        throw "Expected at least 6 status lines, got $($lines.Count)."
    }
    if ($lines[1] -notmatch "dock=2") {
        Write-Host $out
        throw "Dock right failed: $($lines[1])"
    }
    if ($lines[2] -notmatch "hidden=True") {
        Write-Host $out
        throw "Hide failed: $($lines[2])"
    }
    if ($lines[3] -notmatch "hidden=False") {
        Write-Host $out
        throw "Show failed: $($lines[3])"
    }
    if ($lines[4] -notmatch "lang=en") {
        Write-Host $out
        throw "English switch failed: $($lines[4])"
    }
    if ($lines[5] -notmatch "lang=zh-CN") {
        Write-Host $out
        throw "Chinese switch failed: $($lines[5])"
    }

    Write-Host "PASS"
    Write-Host $lines[0]
    Write-Host $lines[1]
    Write-Host $lines[2]
    Write-Host $lines[3]
    Write-Host $lines[4]
    Write-Host $lines[5]
    exit 0
}
finally {
    Remove-Item -LiteralPath $exe, $pdb -Force -ErrorAction SilentlyContinue
}
