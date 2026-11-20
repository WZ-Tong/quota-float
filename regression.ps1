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
    $proc.StandardInput.WriteLine("menu")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("eq")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("policy")
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
    $proc.StandardInput.WriteLine("menu")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("lang zh-CN")
    Start-Sleep -Milliseconds 300
    $proc.StandardInput.WriteLine("status")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("menu")
    Start-Sleep -Milliseconds 200
    $proc.StandardInput.WriteLine("quit")

    if (-not $proc.WaitForExit(5000)) {
        $proc.Kill()
        throw "Process did not exit."
    }

    $out = $proc.StandardOutput.ReadToEnd()
    $err = $proc.StandardError.ReadToEnd()
    $lines = @($out -split "`r?`n" | Where-Object { $_ -like "status *" })
    $menus = @($out -split "`r?`n" | Where-Object { $_ -like "menu *" })
    $equations = @($out -split "`r?`n" | Where-Object { $_ -like "eq *" })
    $policies = @($out -split "`r?`n" | Where-Object { $_ -like "policy *" })

    if ($lines.Count -lt 6) {
        Write-Host $out
        Write-Host $err
        throw "Expected at least 6 status lines, got $($lines.Count)."
    }
    if ($lines[0] -notmatch "hz=[1-9][0-9]*") {
        Write-Host $out
        throw "Display refresh rate was not detected: $($lines[0])"
    }
    if ($lines[0] -notmatch "frame=[0-9]+") {
        Write-Host $out
        throw "Frame interval was not reported: $($lines[0])"
    }
    if ($lines[0] -notmatch "capture=excluded") {
        Write-Host $out
        throw "Capture exclusion is not active: $($lines[0])"
    }
    if ($lines[1] -notmatch "dock=2") {
        Write-Host $out
        throw "Dock right failed: $($lines[1])"
    }
    if ($lines[1] -notmatch "interval=120") {
        Write-Host $out
        throw "Visible refresh interval is not 120s: $($lines[1])"
    }
    if ($lines[1] -notmatch "highres=on") {
        Write-Host $out
        throw "High-resolution timing is not active while visible: $($lines[1])"
    }
    if ($lines[2] -notmatch "hidden=True") {
        Write-Host $out
        throw "Hide failed: $($lines[2])"
    }
    if ($lines[2] -notmatch "interval=600") {
        Write-Host $out
        throw "Hidden refresh interval is not 600s: $($lines[2])"
    }
    if ($lines[2] -notmatch "highres=off") {
        Write-Host $out
        throw "High-resolution timing is still active while hidden: $($lines[2])"
    }
    if ($lines[3] -notmatch "hidden=False") {
        Write-Host $out
        throw "Show failed: $($lines[3])"
    }
    if ($lines[3] -notmatch "interval=120") {
        Write-Host $out
        throw "Revealed refresh interval is not 120s: $($lines[3])"
    }
    if ($lines[3] -notmatch "highres=on") {
        Write-Host $out
        throw "High-resolution timing did not resume on reveal: $($lines[3])"
    }
    if ($lines[4] -notmatch "lang=en") {
        Write-Host $out
        throw "English switch failed: $($lines[4])"
    }
    if ($lines[5] -notmatch "lang=zh-CN") {
        Write-Host $out
        throw "Chinese switch failed: $($lines[5])"
    }
    if ($menus.Count -lt 3) {
        Write-Host $out
        throw "Expected 3 menu snapshots, got $($menus.Count)."
    }
    if ($menus[0] -notmatch "English") {
        Write-Host $out
        throw "Chinese menu lacks English switch: $($menus[0])"
    }
    if ($menus[1] -match "English") {
        Write-Host $out
        throw "English menu still has English top-level item: $($menus[1])"
    }
    if ($menus[2] -notmatch "English") {
        Write-Host $out
        throw "Chinese menu did not restore English switch: $($menus[2])"
    }
    if ($equations.Count -lt 1) {
        Write-Host $out
        throw "Equation layout snapshot is missing."
    }
    $symbolMin = 0
    if ($equations[0] -match 'symbolMin=(\d+)') {
        $symbolMin = [int]$Matches[1]
    }
    else {
        Write-Host $out
        throw "Equation symbol minimum width is missing: $($equations[0])"
    }
    if ($equations[0] -match 'requiredWindow=(\d+)') {
        $requiredWindow = [int]$Matches[1]
        if ($lines[0] -match 'bounds=\d+,\d+,(\d+),') {
            $actualWidth = [int]$Matches[1]
            if ($actualWidth -lt $requiredWindow) {
                Write-Host $out
                throw "Window width $actualWidth is below equation requirement $requiredWindow."
            }
        }
    }
    else {
        Write-Host $out
        throw "Equation required window width is missing: $($equations[0])"
    }
    if ($equations[0] -notmatch 'plus=(\d+),(\d+),"\+"') {
        Write-Host $out
        throw "Equation plus symbol is missing or clipped: $($equations[0])"
    }
    if ([int]$Matches[2] -lt $symbolMin) {
        Write-Host $out
        throw "Equation plus symbol width is too small: $($equations[0])"
    }
    if ($equations[0] -notmatch 'equals=(\d+),(\d+),"="') {
        Write-Host $out
        throw "Equation equals symbol is missing or clipped: $($equations[0])"
    }
    if ([int]$Matches[2] -lt $symbolMin) {
        Write-Host $out
        throw "Equation equals symbol width is too small: $($equations[0])"
    }
    if ($equations[0] -notmatch 'balanceFit=True' -or
        $equations[0] -notmatch 'spentFit=True' -or
        $equations[0] -notmatch 'totalFit=True') {
        Write-Host $out
        throw "Equation value text is ellipsized: $($equations[0])"
    }
    if ($policies.Count -lt 1) {
        Write-Host $out
        throw "Refresh policy snapshot is missing."
    }
    $expectedPolicy = @{ v0 = 120; v1 = 30; v2 = 60; v3 = 120; v4 = 120;
                         h0 = 600; h1 = 30; h2 = 60; h3 = 120; h4 = 240 }
    foreach ($key in $expectedPolicy.Keys) {
        if ($policies[0] -notmatch ($key + '=(\d+)')) {
            Write-Host $out
            throw "Refresh policy field missing: $key"
        }
        if ([int]$Matches[1] -ne $expectedPolicy[$key]) {
            Write-Host $out
            throw "Refresh policy mismatch: $key=$($Matches[1]) expected=$($expectedPolicy[$key])"
        }
    }

    Write-Host "PASS"
    Write-Host $lines[0]
    Write-Host $lines[1]
    Write-Host $lines[2]
    Write-Host $lines[3]
    Write-Host $lines[4]
    Write-Host $lines[5]
    Write-Host $menus[0]
    Write-Host $menus[1]
    Write-Host $menus[2]
    exit 0
}
finally {
    Remove-Item -LiteralPath $exe, $pdb -Force -ErrorAction SilentlyContinue
}
