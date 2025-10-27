[CmdletBinding()]
param(
    [string]$Version,
    [string]$InstallDir
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $InstallDir) {
    $InstallDir = Join-Path $repoRoot '.dotnet'
}

$globalJsonPath = Join-Path $repoRoot 'global.json'

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
}

$installerPath = Join-Path $InstallDir 'dotnet-install.ps1'
if (-not (Test-Path $installerPath)) {
    Write-Host "Downloading dotnet installer to $installerPath"
    $invokeParams = @{ Uri = 'https://dot.net/v1/dotnet-install.ps1'; OutFile = $installerPath }
    $command = Get-Command Invoke-WebRequest
    if ($command.Parameters.ContainsKey('UseBasicParsing')) {
        $invokeParams['UseBasicParsing'] = $true
    }
    Invoke-WebRequest @invokeParams
}

$installParams = @{
    InstallDir = $InstallDir
    NoPath     = $true
}

if ($Version) {
    $installParams['Version'] = $Version
}
elseif (Test-Path $globalJsonPath) {
    try {
        $globalJson = Get-Content -Raw -Path $globalJsonPath | ConvertFrom-Json
        $sdkSection = $null
        $sdkProperty = $globalJson.PSObject.Properties['sdk']
        if ($sdkProperty) {
            $sdkSection = $sdkProperty.Value
        }

        $sdkVersion = $null
        if ($sdkSection) {
            $versionProperty = $sdkSection.PSObject.Properties['version']
            if ($versionProperty) {
                $sdkVersion = $versionProperty.Value
            }
        }

        if ([string]::IsNullOrWhiteSpace($sdkVersion)) {
            throw "Unable to determine the SDK version from '$globalJsonPath'"
        }

        $installParams['Version'] = $sdkVersion

        $sdkQuality = $null
        if ($sdkSection) {
            $qualityProperty = $sdkSection.PSObject.Properties['quality']
            if ($qualityProperty) {
                $sdkQuality = $qualityProperty.Value
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($sdkQuality)) {
            $installParams['Quality'] = $sdkQuality
        }
    }
    catch {
        throw "Failed to parse '$globalJsonPath'. $($_.Exception.Message)"
    }
}
else {
    $installParams['Version'] = '8.0.100'
}

& $installerPath @installParams
