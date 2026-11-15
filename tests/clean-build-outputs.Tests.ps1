$ErrorActionPreference = "Stop"

$fixture = Join-Path ([IO.Path]::GetTempPath()) ("DbEx-clean-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $fixture -ErrorAction Stop | Out-Null

try {
    $script = Join-Path $fixture "clean-build-outputs.ps1"
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\clean-build-outputs.ps1") -Destination $script

    $project = Join-Path $fixture "src\Example"
    $debug = Join-Path $project "obj\Debug"
    $custom = Join-Path $project "bin\Custom.Release"
    New-Item -ItemType Directory -Path $debug, $custom -Force | Out-Null
    $source = Join-Path $project "source.cs"
    $assets = Join-Path $project "obj\project.assets.json"
    Set-Content -LiteralPath $source -Value "source"
    Set-Content -LiteralPath $assets -Value "assets"

    foreach ($invalid in @("", " ", ".", "..", ".. ", "Debug.", "../Debug", "..\Debug", "/tmp", "C:\temp", "C:temp")) {
        $rejected = $false
        try {
            & $script -Configuration @("Debug", $invalid)
        }
        catch {
            if ($_.Exception.Message -notmatch "Invalid configuration directory name") { throw }
            $rejected = $true
        }
        if (-not $rejected) { throw "Configuration '$invalid' was not rejected." }
        if (-not (Test-Path -LiteralPath $source) -or -not (Test-Path -LiteralPath $assets) -or -not (Test-Path -LiteralPath $debug)) {
            throw "An invalid configuration caused files to be removed."
        }
    }

    $rejected = $false
    try { & $script -Configuration @() }
    catch {
        if ($_.Exception.Message -notmatch "At least one configuration") { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw "An empty configuration list was not rejected." }

    & $script -Configuration "Custom.Release" -WhatIf
    if (-not (Test-Path -LiteralPath $custom)) { throw "WhatIf removed the custom configuration." }
    & $script -Configuration "Custom.Release"
    if (Test-Path -LiteralPath $custom) { throw "The custom configuration was not removed." }
    & $script
    if ((Test-Path -LiteralPath $debug) -or -not (Test-Path -LiteralPath $source) -or -not (Test-Path -LiteralPath $assets)) {
        throw "Default cleanup did not preserve source and restore assets."
    }
    Write-Output "Build-output cleanup tests passed."
}
finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction Stop
}
