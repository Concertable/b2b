[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory,

    [Parameter(Mandatory)]
    [string] $ImageDirectory,

    [Parameter(Mandatory)]
    [string] $EvidenceDirectory
)

$ErrorActionPreference = 'Stop'

$syftImage = 'anchore/syft:v1.51.1@sha256:95fe0835e5bebc6f8b1f8acef68d47d63d594ef4c0f25c097ff853b23cbac74c'
$trivyImage = 'aquasec/trivy:0.74.0@sha256:62b1e65e8869bc4b4c6aa4fa2b21595256c7c2f6018a9d9ad61caf87187c1969'
$expectedPackageIds = @(
    'Concertable.B2B.Admin.Contracts'
    'Concertable.B2B.Application.Contracts'
    'Concertable.B2B.Artist.Contracts'
    'Concertable.B2B.Booking.Contracts'
    'Concertable.B2B.Concert.Contracts'
    'Concertable.B2B.Deal.Contracts'
    'Concertable.B2B.Hosting'
    'Concertable.B2B.Seed.Contracts'
    'Concertable.B2B.Tenant.Contracts'
    'Concertable.B2B.TestKit'
    'Concertable.B2B.User.Contracts'
    'Concertable.B2B.Venue.Contracts'
)
$expectedImageNames = @(
    'b2b-seeding-simulator.tar.gz'
    'b2b-web.tar.gz'
    'b2b-workers.tar.gz'
)

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$pathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$resolvedEvidenceDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidenceDirectory))
if (-not $resolvedEvidenceDirectory.StartsWith("$artifactsRoot$([System.IO.Path]::DirectorySeparatorChar)", $pathComparison)) {
    throw 'EvidenceDirectory must be a child of the repository artifacts directory.'
}
if ([System.IO.Directory]::Exists($resolvedEvidenceDirectory)) {
    Remove-Item -LiteralPath $resolvedEvidenceDirectory -Recurse -Force
}
[System.IO.Directory]::CreateDirectory($resolvedEvidenceDirectory) | Out-Null

try {
    $resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
    $resolvedImageDirectory = (Resolve-Path -LiteralPath $ImageDirectory).Path
    $packages = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -Filter '*.nupkg' -File |
        Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [StringComparison]::OrdinalIgnoreCase) })
    $images = @(Get-ChildItem -LiteralPath $resolvedImageDirectory -Filter '*.tar.gz' -File)

    if ($packages.Count -ne $expectedPackageIds.Count) {
        throw "Expected $($expectedPackageIds.Count) NuGet candidates, found $($packages.Count)."
    }

    foreach ($packageId in $expectedPackageIds) {
        $matchingPackages = @($packages | Where-Object {
            $_.Name.StartsWith("$packageId.", [StringComparison]::Ordinal) })
        if ($matchingPackages.Count -ne 1) {
            throw "Expected exactly one '$packageId' candidate, found $($matchingPackages.Count)."
        }
    }

    $actualImageNames = @($images.Name | Sort-Object)
    $expectedSortedImageNames = @($expectedImageNames | Sort-Object)
    if (Compare-Object -ReferenceObject $expectedSortedImageNames -DifferenceObject $actualImageNames -CaseSensitive) {
        throw "OCI candidate names do not match the expected B2B set: $($actualImageNames -join ', ')."
    }

    $candidatePaths = [System.Collections.Generic.List[string]]::new()
    foreach ($candidate in @($packages) + @($images)) {
        $candidatePaths.Add([System.IO.Path]::GetRelativePath($repositoryRoot, $candidate.FullName).Replace('\', '/'))
    }
    $candidatePaths.Sort([StringComparer]::Ordinal)

    $manifestLines = foreach ($relativePath in $candidatePaths) {
        $fullPath = Join-Path $repositoryRoot $relativePath
        $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $relativePath"
    }

    $manifestPath = Join-Path $resolvedEvidenceDirectory 'SHA256SUMS'
    $utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($manifestPath, ($manifestLines -join "`n") + "`n", $utf8WithoutBom)

    foreach ($manifestLine in [System.IO.File]::ReadAllLines($manifestPath)) {
        if ($manifestLine -notmatch '^(?<hash>[0-9a-f]{64})  (?<path>.+)$') {
            throw "Invalid SHA-256 manifest line '$manifestLine'."
        }

        $candidatePath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Matches.path))
        if (-not $candidatePath.StartsWith("$repositoryRoot$([System.IO.Path]::DirectorySeparatorChar)", $pathComparison)) {
            throw "Manifest path '$($Matches.path)' escapes the repository root."
        }
        if (-not [System.IO.File]::Exists($candidatePath)) {
            throw "Manifest candidate '$($Matches.path)' is missing."
        }

        $actualHash = (Get-FileHash -LiteralPath $candidatePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -cne $Matches.hash) {
            throw "SHA-256 mismatch for '$($Matches.path)'."
        }
    }

    $sbomDirectory = Join-Path $resolvedEvidenceDirectory 'sboms'
    $scanDirectory = Join-Path $resolvedEvidenceDirectory 'scans'
    $cacheDirectory = Join-Path $repositoryRoot 'artifacts/.integrity-cache/trivy'
    [System.IO.Directory]::CreateDirectory($sbomDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($scanDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($cacheDirectory) | Out-Null

    foreach ($package in $packages | Sort-Object Name) {
        $inputPath = [System.IO.Path]::GetRelativePath($repositoryRoot, $package.FullName).Replace('\', '/')
        $outputPath = [System.IO.Path]::GetRelativePath(
            $repositoryRoot,
            (Join-Path $sbomDirectory "$($package.Name).cdx.json")).Replace('\', '/')
        & docker run --rm --volume "${repositoryRoot}:/workspace" $syftImage scan "file:/workspace/$inputPath" --output "cyclonedx-json=/workspace/$outputPath"
        if ($LASTEXITCODE -ne 0) {
            throw "Syft failed for '$($package.Name)' with exit code $LASTEXITCODE."
        }
    }

    $scanFailed = $false
    foreach ($image in $images | Sort-Object Name) {
        $inputPath = [System.IO.Path]::GetRelativePath($repositoryRoot, $image.FullName).Replace('\', '/')
        $sbomPath = [System.IO.Path]::GetRelativePath(
            $repositoryRoot,
            (Join-Path $sbomDirectory "$($image.Name).cdx.json")).Replace('\', '/')
        $vulnerabilityScanPath = [System.IO.Path]::GetRelativePath(
            $repositoryRoot,
            (Join-Path $scanDirectory "$($image.Name).vulnerabilities.trivy.json")).Replace('\', '/')
        $secretScanPath = [System.IO.Path]::GetRelativePath(
            $repositoryRoot,
            (Join-Path $scanDirectory "$($image.Name).secrets.trivy.json")).Replace('\', '/')

        & docker run --rm --volume "${repositoryRoot}:/workspace" $syftImage scan "/workspace/$inputPath" --output "cyclonedx-json=/workspace/$sbomPath"
        if ($LASTEXITCODE -ne 0) {
            throw "Syft failed for '$($image.Name)' with exit code $LASTEXITCODE."
        }

        & docker run --rm `
            --volume "${repositoryRoot}:/workspace" `
            --volume "${cacheDirectory}:/root/.cache/trivy" `
            $trivyImage image `
            --input "/workspace/$inputPath" `
            --scanners vuln `
            --severity HIGH,CRITICAL `
            --exit-code 1 `
            --format json `
            --output "/workspace/$vulnerabilityScanPath" `
            --ignorefile /dev/null `
            --disable-telemetry `
            --skip-version-check `
            --no-progress
        if ($LASTEXITCODE -ne 0) {
            $scanFailed = $true
        }

        & docker run --rm `
            --volume "${repositoryRoot}:/workspace" `
            --volume "${cacheDirectory}:/root/.cache/trivy" `
            $trivyImage image `
            --input "/workspace/$inputPath" `
            --scanners secret `
            --severity UNKNOWN,LOW,MEDIUM,HIGH,CRITICAL `
            --exit-code 1 `
            --format json `
            --output "/workspace/$secretScanPath" `
            --ignorefile /dev/null `
            --disable-telemetry `
            --skip-version-check `
            --no-progress
        if ($LASTEXITCODE -ne 0) {
            $scanFailed = $true
        }
    }

    $expectedEvidenceFiles = @(
        Get-ChildItem -LiteralPath $sbomDirectory -Filter '*.cdx.json' -File
        Get-ChildItem -LiteralPath $scanDirectory -Filter '*.trivy.json' -File
    )
    $expectedSbomCount = $expectedPackageIds.Count + $expectedImageNames.Count
    $expectedEvidenceCount = $expectedSbomCount + ($expectedImageNames.Count * 2)
    if ($expectedEvidenceFiles.Count -ne $expectedEvidenceCount -or $expectedEvidenceFiles.Where({ $_.Length -eq 0 }).Count -ne 0) {
        throw "Expected $expectedSbomCount non-empty SBOMs and $($expectedImageNames.Count * 2) non-empty Trivy reports."
    }

    if ($scanFailed) {
        throw 'An OCI candidate contains an embedded secret or a High/Critical vulnerability, or a Trivy scan failed.'
    }
}
catch {
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedEvidenceDirectory 'failure.txt'),
        $_.Exception.Message + "`n",
        [System.Text.UTF8Encoding]::new($false))
    throw
}
