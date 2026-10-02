[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [Parameter(Mandatory)][string] $ImageTag,
    [Parameter(Mandatory)][string] $ArchivePath,
    [Parameter(Mandatory)][string] $SourceRevision
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$projectDirectory = Join-Path $repositoryRoot 'api/src/Concertable.B2B.Workers'
$publishDirectory = Join-Path $projectDirectory "bin/$Configuration/net10.0/publish"
$archive = [IO.Path]::GetFullPath($ArchivePath)
$reference = "concertable/b2b-workers:$ImageTag"

& dotnet publish (Join-Path $projectDirectory 'Concertable.B2B.Workers.csproj') `
    --configuration $Configuration --no-restore --output $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Could not publish the Workers application.' }

New-Item -ItemType Directory -Path (Split-Path $archive -Parent) -Force | Out-Null
& docker build --pull --no-cache `
    --file (Join-Path $projectDirectory 'Dockerfile') `
    --label "org.opencontainers.image.source=https://github.com/Concertable/b2b" `
    --label "org.opencontainers.image.revision=$SourceRevision" `
    --label "org.opencontainers.image.version=$ImageTag" `
    --tag $reference $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Could not build the Workers image.' }

& docker image save --output $archive $reference
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archive) -or (Get-Item -LiteralPath $archive).Length -eq 0) {
    throw 'Could not archive the Workers image.'
}
