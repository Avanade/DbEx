<#
    .SYNOPSIS
    Builds NuGet packages and publishes them when server credentials are provided.

    .DESCRIPTION
    Builds and packs all supported target frameworks using Release by default.
    Overrides GeneratePackageOnBuild so packing builds rather than reusing existing binaries.
    Preserves existing packages except when replacing the same package filename.
    Remote publishing uploads only packages produced by this invocation.

    .PARAMETER PublishFolder
    Local folder to publish the built NuGet packages to.

    .PARAMETER Configuration
    Build configuration. Defaults to Release independently of the Visual Studio configuration.

    .PARAMETER IncludeSymbols
    Create a NuGet symbols package.

    .PARAMETER IncludeSource
    Include source in the NuGet package.

    .PARAMETER ApiKey
    API key for publishing to the server.

    .PARAMETER NugetServer
    URL of the NuGet server.

    .PARAMETER ProjectsToPublish
    List of projects to build and publish.

    .PARAMETER DeleteFromlocalCache
    Remove matching newly packed package versions from the local NuGet cache by default.
    Pass -DeleteFromlocalCache:$false to preserve cached copies.

    .PARAMETER NugetCacheFolder
    Path to the NuGet cache folder.

    .INPUTS
    None.

    .EXAMPLE
    C:\PS> .\nuget-publish.ps1 -Configuration Debug -IncludeSymbols -IncludeSource
    Build all packages in Debug and publish locally.

    .EXAMPLE
    C:\PS> .\nuget-publish.ps1 -NugetServer 'https://api.nuget.org/v3/index.json' -ApiKey 'key' -IncludeSymbols -IncludeSource -ProjectsToPublish @('src\DbEx')
    Build selected packages and push to the remote server.
#>
param(
    [string]$PublishFolder = "$($env:USERPROFILE)\nuget-publish",
    [string]$Configuration = 'Release',
    [switch]$IncludeSymbols,
    [switch]$IncludeSource,
    [string]$NugetServer,
    [string]$ApiKey,
    [switch]$DeleteFromlocalCache = $true,
    [string]$NugetCacheFolder = "$($env:USERPROFILE)\.nuget\packages",
    [string[]]$ProjectsToPublish = @(
        "src\DbEx",
        "src\DbEx.MySql",
        "src\DbEx.Postgres",
        "src\DbEx.SqlServer")
)

$hasApiKey = ![string]::IsNullOrWhiteSpace($ApiKey)
$hasNugetServer = ![string]::IsNullOrWhiteSpace($NugetServer)
if ($hasApiKey -ne $hasNugetServer) {
    throw 'ApiKey and NugetServer must both be provided for remote publishing.'
}
$ShouldPublishRemote = $hasApiKey -and $hasNugetServer

if (!(Test-Path -LiteralPath $PublishFolder)) {
    Write-Host "Publish folder doesn't exist - creating folder." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PublishFolder -ErrorAction Stop | Out-Null
}

$stagingFolder = Join-Path ([IO.Path]::GetTempPath()) ("DbEx-nuget-publish-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingFolder -ErrorAction Stop | Out-Null
$packageDetailsRegexp = [regex] '^(?<name>.+?)\.(?<version>\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?)$'

try {
    foreach ($project in $ProjectsToPublish) {
        $projectPath = if ([IO.Path]::IsPathRooted($project)) { $project } else { Join-Path $PSScriptRoot $project }
        $packArguments = @('pack', $projectPath, '-o', $stagingFolder, '-c', $Configuration, '-p:GeneratePackageOnBuild=false', '--force')
        if ($IncludeSource) { $packArguments += '--include-source' }
        if ($IncludeSymbols) { $packArguments += '--include-symbols' }

        Write-Host "`r`nPacking $project ($Configuration, all target frameworks)." -ForegroundColor Yellow
        & dotnet @packArguments
        if ($LASTEXITCODE -ne 0) { throw "Failed to pack $project (exit $LASTEXITCODE)." }
    }

    $packages = @(Get-ChildItem -LiteralPath $stagingFolder -File -ErrorAction Stop |
        Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
    if ($packages.Count -eq 0) { throw 'Packing produced no NuGet packages.' }

    foreach ($package in $packages) {
        Copy-Item -LiteralPath $package.FullName -Destination $PublishFolder -Force -ErrorAction Stop

        if ($DeleteFromlocalCache -and $package.Extension -eq '.nupkg') {
            $packageName = [IO.Path]::GetFileNameWithoutExtension($package.Name)
            $packageDetails = $packageDetailsRegexp.Match($packageName)
            if (!$packageDetails.Success) { throw "Cannot determine cache identity for $($package.Name)." }

            $packageCachePath = [IO.Path]::Combine($NugetCacheFolder,
                $packageDetails.Groups['name'].Value.ToLowerInvariant(),
                $packageDetails.Groups['version'].Value.ToLowerInvariant())
            if (Test-Path -LiteralPath $packageCachePath) {
                Write-Host "Deleting local cache for package`r`n`t$packageCachePath"
                Remove-Item -LiteralPath $packageCachePath -Recurse -ErrorAction Stop
            }
            else {
                Write-Host "Local cache for package does not exist`r`n`t$packageCachePath"
            }
        }
    }

    # dotnet also pushes matching symbol packages.
    if ($ShouldPublishRemote) {
        foreach ($package in $packages | Where-Object { $_.Extension -eq '.nupkg' }) {
            Write-Host "`r`nPublishing $($package.Name) to $NugetServer." -ForegroundColor Yellow
            & dotnet nuget push $package.FullName --source $NugetServer --api-key $ApiKey --skip-duplicate
            if ($LASTEXITCODE -ne 0) { throw "Failed to push $($package.Name) (exit $LASTEXITCODE)." }
        }
    }
}
finally {
    Remove-Item -LiteralPath $stagingFolder -Recurse -Force -ErrorAction Stop
}
