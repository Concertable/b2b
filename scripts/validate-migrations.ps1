<# Fails when any B2B DbContext has drifted from its committed migration snapshot. #>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$manifest = Import-PowerShellDataFile (Join-Path $repositoryRoot 'migrations.psd1')

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to restore repository-local .NET tools.'
}

$savedEnvironment = @{}
try {
    foreach ($name in $manifest.Environment.Keys) {
        $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $manifest.Environment[$name], 'Process')
    }

    foreach ($migration in $manifest.Migrations) {
        $project = Join-Path $repositoryRoot $migration.Project
        $startup = Join-Path $repositoryRoot $migration.StartupProject
        if (-not (Test-Path -LiteralPath $project -PathType Container) -or
            -not (Test-Path -LiteralPath $startup -PathType Container)) {
            throw "Migration project or startup project is missing: $($migration.Context)"
        }

        Write-Host "Validating $($migration.Context) migration snapshot..."
        dotnet ef migrations has-pending-model-changes `
            --context $migration.Context `
            --project $project `
            --startup-project $startup `
            --configuration $Configuration `
            --no-build

        if ($LASTEXITCODE -ne 0) {
            throw "$($migration.Context) has pending model changes or could not be validated."
        }
    }
}
finally {
    foreach ($name in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
    }
}

Write-Output "Validated $($manifest.Migrations.Count) B2B migration snapshots."
