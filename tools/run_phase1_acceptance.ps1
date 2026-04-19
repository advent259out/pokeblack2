[CmdletBinding()]
param(
    [string]$ProjectRoot = '',
    [string]$RomPath,
    [string]$ExportRoot,
    [string]$UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe',
    [string]$TestFilter = 'PokeBlack2.Foundation.Editor.BlackWhiteFoundationSmokeTests'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $ProjectRoot = (Resolve-Path (Join-Path $ScriptRoot '..')).Path
}

if (-not $PSBoundParameters.ContainsKey('RomPath')) {
    $RomPath = Join-Path $ProjectRoot 'ROMs\pokeblack.nds'
}

if (-not $PSBoundParameters.ContainsKey('ExportRoot')) {
    $ExportRoot = Join-Path $ProjectRoot 'External\Exports\BlackWhite\M0'
}

$TempRoot = Join-Path $ProjectRoot 'Temp'
$TestResults = Join-Path $TempRoot 'BlackWhiteFoundationSmokeTests.xml'
$LogFile = Join-Path $TempRoot 'BlackWhiteFoundationSmokeTests.log'

New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Write-Host "==> $Label"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Step '$Label' failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $RomPath)) {
    throw "Expected canonical ROM at '$RomPath'."
}

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity executable was not found at '$UnityExe'."
}

Invoke-Step `
    -Label 'Probe canonical ROM' `
    -FilePath 'python' `
    -Arguments @('-m', 'tools.gen5.probe_rom', '--rom', $RomPath) `
    -WorkingDirectory $ProjectRoot

Invoke-Step `
    -Label 'Prepare export root scaffold' `
    -FilePath 'python' `
    -Arguments @('-m', 'tools.gen5.prepare_export_root', '--root', $ExportRoot) `
    -WorkingDirectory $ProjectRoot

Invoke-Step `
    -Label 'Run Unity EditMode smoke tests' `
    -FilePath $UnityExe `
    -Arguments @(
        '-batchmode',
        '-nographics',
        '-projectPath', $ProjectRoot,
        '-runTests',
        '-runSynchronously',
        '-testPlatform', 'EditMode',
        '-testFilter', $TestFilter,
        '-testResults', $TestResults,
        '-logFile', $LogFile
    ) `
    -WorkingDirectory $ProjectRoot

if (-not (Test-Path -LiteralPath $TestResults)) {
    throw "Unity test run completed without writing '$TestResults'."
}

Write-Host ''
Write-Host 'Phase 1 acceptance passed.'
Write-Host "ROM: $RomPath"
Write-Host "Export root: $ExportRoot"
Write-Host "Test results: $TestResults"
Write-Host "Unity log: $LogFile"
