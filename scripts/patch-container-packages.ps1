[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ArchivePath,
    [Parameter(Mandatory)][string] $Image
)

$ErrorActionPreference = 'Stop'
$archive = [IO.Path]::GetFullPath($ArchivePath)
if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) {
    throw "Container archive '$archive' does not exist."
}

function Get-RuntimeMetadata {
    param([Parameter(Mandatory)][string] $Reference)

    $inspectionJson = & docker image inspect $Reference
    if ($LASTEXITCODE -ne 0) { throw "Could not inspect container image '$Reference'." }
    $configuration = ($inspectionJson | ConvertFrom-Json)[0].Config
    $serialized = [ordered]@{
        Cmd = @($configuration.Cmd)
        Entrypoint = @($configuration.Entrypoint)
        Env = @($configuration.Env)
        Labels = $configuration.Labels
        User = $configuration.User
        WorkingDir = $configuration.WorkingDir
    } | ConvertTo-Json -Depth 8 -Compress
    [pscustomobject]@{
        Serialized = $serialized
        User = [string] $configuration.User
    }
}

$buildDirectory = Join-Path ([IO.Path]::GetTempPath()) "concertable-container-patch-$([Guid]::NewGuid().ToString('N'))"
$patchCommand = @'
apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade apt libapt-pkg6.0t64 \
    && rm -rf /var/lib/apt/lists/*
'@
$verifyCommand = @'
aptVersion="$(dpkg-query -W -f='${Version}' apt)"
libraryVersion="$(dpkg-query -W -f='${Version}' libapt-pkg6.0t64)"
dpkg --compare-versions "$aptVersion" ge 2.8.4ubuntu0.1 \
    && dpkg --compare-versions "$libraryVersion" ge 2.8.4ubuntu0.1
'@

try {
    $loadOutput = @(& docker image load --input $archive)
    if ($LASTEXITCODE -ne 0) { throw "Could not load container archive '$archive'." }

    $loadedImage = $loadOutput |
        ForEach-Object {
            if ($_ -match '^Loaded image: (.+)$') { $Matches[1] }
        } |
        Select-Object -First 1
    if ($null -eq $loadedImage) {
        $references = @($Image, "ghcr.io/$Image", "docker.io/$Image") | Where-Object { $_ -notmatch '^ghcr\.io/ghcr\.io/' }
        $loadedImage = $references |
            Where-Object {
                & docker image inspect $_ 2>$null | Out-Null
                $LASTEXITCODE -eq 0
            } |
            Select-Object -First 1
    }
    if ($null -eq $loadedImage) {
        throw "Container archive '$archive' did not load '$Image'."
    }
    if ($loadedImage -ne $Image) {
        & docker image tag $loadedImage $Image
        if ($LASTEXITCODE -ne 0) { throw "Could not tag loaded container image '$loadedImage' as '$Image'." }
        & docker image rm $loadedImage | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not remove loaded container image tag '$loadedImage'." }
    }
    $runtimeMetadata = Get-RuntimeMetadata -Reference $Image

    New-Item -ItemType Directory -Path $buildDirectory | Out-Null
    $dockerfile = @("FROM $Image")
    if (-not [string]::IsNullOrWhiteSpace($runtimeMetadata.User) -and $runtimeMetadata.User -notin @('0', 'root')) {
        $dockerfile += 'USER 0'
    }
    $dockerfile += "RUN $patchCommand"
    if (-not [string]::IsNullOrWhiteSpace($runtimeMetadata.User) -and $runtimeMetadata.User -notin @('0', 'root')) {
        $dockerfile += "USER $($runtimeMetadata.User)"
    }
    Set-Content -LiteralPath (Join-Path $buildDirectory 'Dockerfile') -Value ($dockerfile -join [Environment]::NewLine) -NoNewline

    & docker build --file (Join-Path $buildDirectory 'Dockerfile') --tag $Image $buildDirectory
    if ($LASTEXITCODE -ne 0) { throw "Could not update apt packages in '$Image'." }
    if ($runtimeMetadata.Serialized -cne (Get-RuntimeMetadata -Reference $Image).Serialized) {
        throw "Container image '$Image' lost runtime metadata while updating apt packages."
    }

    & docker run --rm --entrypoint /bin/sh $Image -c $verifyCommand
    if ($LASTEXITCODE -ne 0) { throw "Container image '$Image' does not contain the fixed apt packages." }

    & docker image save --output $archive $Image
    if ($LASTEXITCODE -ne 0 -or (Get-Item -LiteralPath $archive).Length -eq 0) {
        throw "Could not archive updated container image '$Image'."
    }
}
finally {
    Remove-Item -LiteralPath $buildDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
