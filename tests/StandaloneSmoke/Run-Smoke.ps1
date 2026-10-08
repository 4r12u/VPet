param(
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [string]$DotNet = 'dotnet',
    [string]$SandboxDirectory = (Join-Path $env:TEMP ('VPet-StandaloneSmoke-' + [guid]::NewGuid().ToString('N')))
)

$ErrorActionPreference = 'Stop'
$taskPublish = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (Test-Path -LiteralPath $SandboxDirectory) {
    throw 'SandboxDirectory must be a new directory. Existing data is never overwritten.'
}
if (!(Test-Path -LiteralPath (Join-Path $taskPublish 'VPet-Simulator.Windows.exe'))) {
    throw 'Expected the complete published application directory.'
}

& $DotNet build (Join-Path $PSScriptRoot 'StandaloneSmoke.csproj') -c Release "-p:PublishDirectory=$taskPublish"
if ($LASTEXITCODE -ne 0) { throw 'Smoke harness build failed.' }
New-Item -ItemType Directory -Path $SandboxDirectory | Out-Null
$taskSandbox = (Resolve-Path -LiteralPath $SandboxDirectory).Path
robocopy $taskPublish $taskSandbox /E /MT:16 /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw 'Sandbox copy failed.' }
New-Item -ItemType File -Path (Join-Path $taskSandbox '.standalone-smoke-sandbox') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/Release/net10.0-windows/StandaloneSmoke.dll') -Destination $taskSandbox

foreach ($taskProfile in @('', 'March 7th')) {
    foreach ($taskPhase in @('init', 'restore')) {
        & $DotNet exec --runtimeconfig (Join-Path $taskSandbox 'VPet-Simulator.Windows.runtimeconfig.json') `
            --depsfile (Join-Path $taskSandbox 'VPet-Simulator.Windows.deps.json') `
            (Join-Path $taskSandbox 'StandaloneSmoke.dll') $taskPhase "prefix#$($taskProfile):|"
        if ($LASTEXITCODE -ne 0) { throw "Smoke check failed: $taskPhase / $taskProfile. Read smoke-*.txt in $taskSandbox." }
    }
}
Get-ChildItem -LiteralPath $taskSandbox -Filter 'smoke-*.txt' | ForEach-Object { Get-Content -LiteralPath $_.FullName }
Write-Output "Smoke checks passed. Sandbox retained for inspection: $taskSandbox"
