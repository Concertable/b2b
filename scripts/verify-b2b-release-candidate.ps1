[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath,

    [switch] $KeepArtifacts,

    # A pull_request run builds the merge ref, so its MinVer height is one no publish will ever emit.
    # Set this there to check the version is well-formed and above the floor without pinning it.
    [switch] $AllowUnpublishableVersion
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'VulnerabilityGate.ps1')

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryUrl = 'https://github.com/Concertable/b2b'
$versionFloor = [version] '0.2.0'
$manifestPath = Join-Path $repositoryRoot '.github/b2b-promotion-candidates.json'

$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) {
    throw 'Could not resolve the B2B source revision.'
}

$workingTreeChanges = @(& git -C $repositoryRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the B2B working tree.'
}
if ($workingTreeChanges.Count -gt 0) {
    throw 'Refusing to prepare a B2B release candidate from a dirty or untracked working tree.'
}

$promotion = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json -Depth 10
if ($promotion.schemaVersion -ne 1) {
    throw "Unsupported promotion manifest schema version '$($promotion.schemaVersion)'."
}
$packageCandidates = @($promotion.nuget)
$imageCandidates = @($promotion.oci)
if ($packageCandidates.Count -eq 0 -or $imageCandidates.Count -eq 0) {
    throw 'The promotion manifest names no candidates.'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($KeepArtifacts) {
        throw 'OutputPath is required when KeepArtifacts is specified.'
    }

    $releaseRoot = Join-Path ([System.IO.Path]::GetTempPath()) "concertable-b2b-release-candidate-$([Guid]::NewGuid().ToString('N'))"
}
elseif ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $releaseRoot = [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    $releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
}

$repositoryPrefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if ($releaseRoot.Equals($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $releaseRoot.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release-candidate output must be outside the B2B repository so the source scan sees only source.'
}
if (Test-Path -LiteralPath $releaseRoot) {
    throw "Release-candidate output already exists: '$releaseRoot'."
}

$releaseId = [Guid]::NewGuid().ToString('N')
$markerPath = Join-Path $releaseRoot '.b2b-release-candidate'
$packageRoot = Join-Path $releaseRoot 'packages'
$imageRoot = Join-Path $releaseRoot 'images'
$evidenceRoot = Join-Path $releaseRoot 'evidence'
$scanRoot = Join-Path $releaseRoot 'scan-input'
$sourceRoot = Join-Path $releaseRoot 'source'
$releaseManifestPath = Join-Path $releaseRoot 'release-manifest.json'
$trivyImage = 'aquasec/trivy:0.74.0@sha256:62b1e65e8869bc4b4c6aa4fa2b21595256c7c2f6018a9d9ad61caf87187c1969'
$trivyCacheVolume = "concertable-b2b-trivy-cache-$releaseId"

$packageToken = $env:GITHUB_PACKAGES_TOKEN
$trivyCacheVolumeCreated = $false
$releaseRootCreated = $false
$loadedImages = [System.Collections.Generic.List[string]]::new()
$completed = $false
$releaseVersion = ''

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Value
    )

    [System.IO.File]::WriteAllText($Path, $Value, [System.Text.UTF8Encoding]::new($false))
}

function Assert-ReleaseRoot {
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Refusing release-candidate operation without marker '$markerPath'."
    }
}

function Get-NuGetIdentity {
    param([Parameter(Mandatory)][System.IO.FileInfo] $Package)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Package.FullName)
    try {
        $manifestEntry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        if ($null -eq $manifestEntry) {
            throw "Package '$($Package.Name)' does not contain a NuGet manifest."
        }

        $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
        try {
            [xml] $manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $manifest.package.metadata
        if ([string] $metadata.repository.url -ne $repositoryUrl -or [string] $metadata.projectUrl -ne $repositoryUrl) {
            throw "Package '$($Package.Name)' does not identify the canonical B2B repository."
        }
        if ([string] $metadata.repository.commit -ne $revision) {
            throw "Package '$($Package.Name)' does not identify exact B2B revision '$revision'."
        }
        if ([string] $metadata.readme -ne 'README.md') {
            throw "Package '$($Package.Name)' does not declare its package README."
        }
        if ([string]::IsNullOrWhiteSpace([string] $metadata.description) -or [string] $metadata.description -eq 'Package Description') {
            throw "Package '$($Package.Name)' does not have a meaningful description."
        }
        if (-not ($archive.Entries | Where-Object FullName -ceq 'README.md')) {
            throw "Package '$($Package.Name)' declares a README it does not contain."
        }

        return [ordered]@{ Id = [string] $metadata.id; Version = [string] $metadata.version }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-ArtifactRecord {
    param([Parameter(Mandatory)][string] $Path)

    $item = Get-Item -LiteralPath $Path
    $releasePrefix = $releaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $item.FullName.StartsWith($releasePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Artifact '$($item.FullName)' is outside the release-candidate root."
    }

    $stream = [System.IO.File]::OpenRead($item.FullName)
    try {
        $hasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            $sha256 = ([System.BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $hasher.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    return [ordered]@{
        path = $item.FullName.Substring($releasePrefix.Length).Replace('\', '/')
        sha256 = $sha256
        bytes = $item.Length
    }
}

function Invoke-Trivy {
    param([Parameter(Mandatory)][string[]] $Arguments)

    & docker run --rm `
        --volume "${sourceRoot}:/work:ro" `
        --volume "${scanRoot}:/scan:ro" `
        --volume "${evidenceRoot}:/evidence" `
        --volume "${trivyCacheVolume}:/root/.cache/trivy" `
        $trivyImage `
        @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Trivy failed with exit code $LASTEXITCODE for arguments '$($Arguments -join ' ')'."
    }
}

function New-TrivyCacheVolume {
    $created = (& docker volume create --label "com.concertable.b2b.release-candidate=$releaseId" $trivyCacheVolume).Trim()
    if ($LASTEXITCODE -ne 0 -or $created -ne $trivyCacheVolume) {
        throw "Could not create owned Trivy cache volume '$trivyCacheVolume'."
    }
}

function Remove-TrivyCacheVolume {
    $inspectionJson = & docker volume inspect $trivyCacheVolume 2>$null
    if ($LASTEXITCODE -ne 0) {
        return
    }

    $inspection = ($inspectionJson | ConvertFrom-Json)[0]
    if ($inspection.Labels.'com.concertable.b2b.release-candidate' -ne $releaseId) {
        throw "Refusing to remove unowned Trivy cache volume '$trivyCacheVolume'."
    }

    & docker volume rm $trivyCacheVolume | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not remove Trivy cache volume '$trivyCacheVolume'."
    }
}

function Remove-LoadedImage {
    param([Parameter(Mandatory)][string] $Image)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $inspectionJson = & docker image inspect $Image 2>$null
        $exit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if ($exit -ne 0) {
        return
    }

    $inspection = ($inspectionJson | ConvertFrom-Json)[0]
    if ($inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision) {
        throw "Refusing to remove unowned release-candidate image '$Image'."
    }

    & docker image rm --force $Image | Out-Null
}

try {
    if ([string]::IsNullOrWhiteSpace($packageToken)) {
        throw 'GITHUB_PACKAGES_TOKEN is required for B2B release-candidate verification.'
    }

    New-Item -ItemType Directory -Path $releaseRoot | Out-Null
    $releaseRootCreated = $true
    try {
        Write-Utf8NoBom -Path $markerPath -Value 'Concertable.B2B release candidate'
    }
    catch {
        Remove-Item -LiteralPath $releaseRoot -Recurse -Force
        $releaseRootCreated = $false
        throw
    }
    New-Item -ItemType Directory -Path $packageRoot, $imageRoot, $evidenceRoot, $scanRoot, $sourceRoot | Out-Null

    # ---- packages -------------------------------------------------------------------------------

    & dotnet restore (Join-Path $repositoryRoot 'Concertable.B2B.slnx')
    if ($LASTEXITCODE -ne 0) {
        throw "B2B restore failed with exit code $LASTEXITCODE."
    }

    foreach ($candidate in $packageCandidates) {
        $project = Join-Path $repositoryRoot $candidate.project
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw "Promotion candidate project '$($candidate.project)' does not exist."
        }

        & dotnet build $project --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "B2B package build failed for '$($candidate.project)' with exit code $LASTEXITCODE."
        }

        & dotnet pack $project --configuration $Configuration --no-build --no-restore --output $packageRoot
        if ($LASTEXITCODE -ne 0) {
            throw "B2B package creation failed for '$($candidate.project)' with exit code $LASTEXITCODE."
        }
    }

    & (Join-Path $PSScriptRoot 'verify-package-candidates.ps1') -PackageDirectory $packageRoot

    $packages = @(Get-ChildItem -LiteralPath $packageRoot -Filter '*.nupkg' -File |
        Where-Object Name -NotLike '*.symbols.nupkg')
    $packageRecords = @($packages | ForEach-Object {
        $identity = Get-NuGetIdentity -Package $_
        [ordered]@{
            id = $identity.Id
            version = $identity.Version
            artifact = Get-ArtifactRecord -Path $_.FullName
        }
    } | Sort-Object { $_.id })

    $expectedPackageIds = @($packageCandidates | ForEach-Object { $_.id } | Sort-Object)
    $actualPackageIds = @($packageRecords | ForEach-Object { $_.id })
    if (($actualPackageIds -join ',') -ne ($expectedPackageIds -join ',')) {
        throw "Release-candidate package set '$($actualPackageIds -join ',')' does not match the promotion manifest."
    }

    $versions = @($packageRecords | ForEach-Object { $_.version } | Sort-Object -Unique)
    if ($versions.Count -ne 1 -or $versions[0] -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
        throw "B2B packages do not share one valid SemVer version: '$($versions -join ',')'."
    }
    $releaseVersion = $versions[0]
    if ([version] ($releaseVersion.Split('-', 2)[0]) -lt $versionFloor) {
        throw "Release-candidate version '$releaseVersion' is below the $versionFloor floor that clears the monorepo's published 0.1 train."
    }

    # ---- images ---------------------------------------------------------------------------------

    # ContainerArchiveOutputPath is what keeps this local: it overrides ContainerRegistry, which
    # Directory.Build.props defaults to ghcr.io. Never drop it here — see TECH_DEBT.md.
    $imageEvidence = @()
    foreach ($candidate in $imageCandidates) {
        $project = Join-Path $repositoryRoot $candidate.project
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw "Promotion candidate project '$($candidate.project)' does not exist."
        }

        $repository = $candidate.repository -replace '^ghcr\.io/', ''
        $archivePath = Join-Path $imageRoot $candidate.archive
        $stem = [System.IO.Path]::GetFileNameWithoutExtension($candidate.archive) -replace '\.tar$', ''

        & dotnet publish $project `
            --configuration $Configuration `
            --no-restore `
            -t:PublishContainer `
            -p:ContainerRepository=$repository `
            -p:ContainerImageTag=$releaseVersion `
            -p:ContainerArchiveOutputPath=$archivePath
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archivePath) -or (Get-Item -LiteralPath $archivePath).Length -eq 0) {
            throw "Could not build release-candidate image for '$($candidate.project)'."
        }

        $imageEvidence += [ordered]@{
            Candidate = $candidate
            Repository = $candidate.repository
            LocalReference = "${repository}:$releaseVersion"
            Stem = $stem
            ArchivePath = $archivePath
        }
    }

    New-TrivyCacheVolume
    $trivyCacheVolumeCreated = $true

    # Scan tracked source, not the working tree. git archive materialises exactly what is committed,
    # so the scan sees the files that ship and none of the build output that --skip-dirs has to exclude
    # by hand — which measurably did not work: the same scan over the working tree ran past ten minutes
    # with bin, obj and node_modules excluded, and over tracked source it is under two.
    & git -C $repositoryRoot archive --format=tar $revision | & tar -x -C $sourceRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not materialise tracked source for the secret scan.'
    }

    Invoke-Trivy -Arguments @(
        'filesystem', '--scanners', 'secret', '--exit-code', '1', '--format', 'json',
        '--output', '/evidence/source-secrets.json', '--no-progress', '/work'
    )

    $scanFailed = $false
    foreach ($item in $imageEvidence) {
        $archive = Get-Item -LiteralPath $item.ArchivePath

        # A Functions host's base image is OCI-format, so PublishContainer writes an OCI layout that
        # Trivy can only read as a directory. docker save writes a hybrid carrying manifest.json too,
        # which it reads in place — so the condition is the absence of manifest.json.
        $expanded = Expand-OciArchive -Archive $archive -Destination (Join-Path $scanRoot $item.Stem)
        $scanInput = if ($null -eq $expanded) { "/scan/$($archive.Name)" } else { "/scan/$($item.Stem)" }
        if ($null -eq $expanded) {
            Copy-Item -LiteralPath $archive.FullName -Destination (Join-Path $scanRoot $archive.Name)
        }

        Invoke-Trivy -Arguments @(
            'image', '--scanners', 'vuln', '--severity', 'HIGH,CRITICAL', '--exit-code', '0',
            '--format', 'json', '--output', "/evidence/$($item.Stem)-vulnerabilities.json",
            '--ignorefile', '/dev/null', '--no-progress', '--input', $scanInput
        )
        Invoke-Trivy -Arguments @(
            'image', '--scanners', 'secret', '--severity', 'UNKNOWN,LOW,MEDIUM,HIGH,CRITICAL',
            '--exit-code', '1', '--format', 'json', '--output', "/evidence/$($item.Stem)-secrets.json",
            '--ignorefile', '/dev/null', '--no-progress', '--input', $scanInput
        )
        Invoke-Trivy -Arguments @(
            'image', '--format', 'cyclonedx', '--output', "/evidence/$($item.Stem).cdx.json",
            '--no-progress', '--input', $scanInput
        )

        $vulnerabilityPath = Join-Path $evidenceRoot "$($item.Stem)-vulnerabilities.json"
        foreach ($finding in Get-BlockingVulnerabilities -ReportPath $vulnerabilityPath) {
            Write-Host "::error::$($item.Stem): $($finding.Severity) $($finding.Id) in $($finding.Package) $($finding.Version), fixed in $($finding.FixedVersion)."
            $scanFailed = $true
        }

        $sbomPath = Join-Path $evidenceRoot "$($item.Stem).cdx.json"
        $sbom = Get-Content -Raw -LiteralPath $sbomPath | ConvertFrom-Json
        if ($sbom.bomFormat -ne 'CycloneDX' -or @($sbom.components).Count -eq 0) {
            throw "Release-candidate SBOM validation failed for '$($item.Stem)'."
        }

        & docker load --input $archive.FullName | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not load release-candidate image '$($item.LocalReference)'."
        }
        $loadedImages.Add($item.LocalReference)

        $inspectionJson = & docker image inspect $item.LocalReference
        if ($LASTEXITCODE -ne 0) {
            throw "Could not inspect release-candidate image '$($item.LocalReference)'."
        }
        $inspection = ($inspectionJson | ConvertFrom-Json)[0]
        $configuredEnvironment = @($inspection.Config.Env) -join "`n"
        if ($configuredEnvironment -match 'GITHUB_PACKAGES_TOKEN') {
            throw "Image '$($item.LocalReference)' retains the package-token environment variable."
        }

        $item.LocalImageId = [string] $inspection.Id
        $item.Archive = Get-ArtifactRecord -Path $archive.FullName
        $item.Sbom = Get-ArtifactRecord -Path $sbomPath
        $item.Vulnerabilities = Get-ArtifactRecord -Path $vulnerabilityPath
        $item.Secrets = Get-ArtifactRecord -Path (Join-Path $evidenceRoot "$($item.Stem)-secrets.json")
    }

    if ($scanFailed) {
        throw 'A release-candidate image carries a blocking vulnerability; see the errors above.'
    }

    # ---- manifest -------------------------------------------------------------------------------

    $imageRecords = @($imageEvidence | ForEach-Object {
        [ordered]@{
            repository = $_.Repository
            version = $releaseVersion
            sourceRevision = $revision
            intendedTags = @($releaseVersion, $revision)
            localImageId = $_.LocalImageId
            archive = $_.Archive
            sbom = $_.Sbom
            vulnerabilityScan = $_.Vulnerabilities
            allSeveritySecretScan = $_.Secrets
        }
    })

    $manifest = [ordered]@{
        schemaVersion = 1
        repository = $repositoryUrl
        sourceRevision = $revision
        version = $releaseVersion
        versionIsPublishable = -not $AllowUnpublishableVersion.IsPresent
        packages = $packageRecords
        images = $imageRecords
        sourceSecretScan = Get-ArtifactRecord -Path (Join-Path $evidenceRoot 'source-secrets.json')
    }
    Write-Utf8NoBom -Path $releaseManifestPath -Value ($manifest | ConvertTo-Json -Depth 12)

    $verified = Get-Content -Raw -LiteralPath $releaseManifestPath | ConvertFrom-Json
    if ($verified.sourceRevision -ne $revision -or
        $verified.version -ne $releaseVersion -or
        @($verified.packages).Count -ne $packageCandidates.Count -or
        @($verified.images).Count -ne $imageCandidates.Count) {
        throw 'B2B release-candidate manifest validation failed.'
    }

    $completed = $true
    Write-Host "Verified B2B release candidate $releaseVersion for revision ${revision}: $($packageRecords.Count) packages, $($imageRecords.Count) images, manifest and evidence complete."
    if ($AllowUnpublishableVersion) {
        Write-Host 'Version was accepted as shape-only: this ref is not the one that publishes.'
    }
    if ($KeepArtifacts) {
        Write-Host "Retained release-candidate artifacts at '$releaseRoot'."
    }
}
finally {
    foreach ($image in $loadedImages) {
        Remove-LoadedImage -Image $image
    }

    try {
        if ($trivyCacheVolumeCreated) {
            Remove-TrivyCacheVolume
        }
    }
    finally {
        if ((Test-Path -LiteralPath $markerPath -PathType Leaf) -and (-not $KeepArtifacts -or -not $completed)) {
            Assert-ReleaseRoot
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force
            $releaseRootCreated = $false
        }
        elseif ($releaseRootCreated -and -not (Test-Path -LiteralPath $markerPath)) {
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force -ErrorAction SilentlyContinue
            $releaseRootCreated = $false
        }
    }
}
