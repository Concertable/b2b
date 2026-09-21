<# Restores and builds every B2B package candidate from a clean consumer that sees no repository source. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $PSScriptRoot '..' '.github' 'b2b-promotion-candidates.json'
$promotion = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json -Depth 10
$expectedPackageIds = @($promotion.nuget | ForEach-Object { $_.id })
if ($expectedPackageIds.Count -eq 0) {
    throw 'The B2B promotion manifest selects no NuGet candidates.'
}

$resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packages = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -Filter '*.nupkg' -File |
    Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [StringComparison]::OrdinalIgnoreCase) })

if ($packages.Count -ne $expectedPackageIds.Count) {
    throw "Expected $($expectedPackageIds.Count) package candidates, found $($packages.Count)."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$metadata = foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspec = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [StringComparison]::OrdinalIgnoreCase) })
        if ($nuspec.Count -ne 1) {
            throw "Package '$($package.Name)' contains $($nuspec.Count) nuspec files."
        }

        $stream = $nuspec[0].Open()
        try {
            $document = [System.Xml.XmlDocument]::new()
            $document.Load($stream)
            $metadataNode = $document.DocumentElement.ChildNodes |
                Where-Object { $_.LocalName -eq 'metadata' } |
                Select-Object -First 1
            $idNode = $metadataNode.ChildNodes | Where-Object { $_.LocalName -eq 'id' } | Select-Object -First 1
            $versionNode = $metadataNode.ChildNodes | Where-Object { $_.LocalName -eq 'version' } | Select-Object -First 1
            [pscustomobject]@{ Id = $idNode.InnerText; Version = $versionNode.InnerText }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

$actualPackageIds = @($metadata.Id | Sort-Object)
$expectedSorted = @($expectedPackageIds | Sort-Object)
if (Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actualPackageIds) {
    throw "Package candidate IDs do not match the current B2B candidate set: $($actualPackageIds -join ', ')."
}

$versions = @($metadata.Version | Sort-Object -Unique)
if ($versions.Count -ne 1) {
    throw "B2B package candidates must use one version; found $($versions -join ', ')."
}

$consumerDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "concertable-b2b-package-consumer-$([Guid]::NewGuid().ToString('N'))"
[System.IO.Directory]::CreateDirectory($consumerDirectory) | Out-Null

try {
    $escapedPackageDirectory = [System.Security.SecurityElement]::Escape($resolvedPackageDirectory)
    $escapedVersion = [System.Security.SecurityElement]::Escape($versions[0])
    $projectPath = Join-Path $consumerDirectory 'Consumer.csproj'
    $sourcePath = Join-Path $consumerDirectory 'Program.cs'
    $configPath = Join-Path $consumerDirectory 'nuget.config'

    $packageReferences = ($expectedPackageIds | ForEach-Object {
        "    <PackageReference Include=`"$_`" Version=`"$escapedVersion`" />"
    }) -join "`n"
    $packagePatterns = ($expectedPackageIds | ForEach-Object {
        "      <package pattern=`"$_`" />"
    }) -join "`n"

    [System.IO.File]::WriteAllText($projectPath, @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
$packageReferences
  </ItemGroup>
</Project>
"@)

    [System.IO.File]::WriteAllText($sourcePath, @'
var boundTypes = new[]
{
    typeof(Concertable.B2B.Admin.Contracts.IAdminModule),
    typeof(Concertable.B2B.Application.Contracts.AcceptedApplication),
    typeof(Concertable.B2B.Artist.Contracts.ArtistProfile),
    typeof(Concertable.B2B.Authorization.Contracts.MembershipSnapshot),
    typeof(Concertable.B2B.Booking.Contracts.ConfirmedBookingSnapshot),
    typeof(Concertable.B2B.Concert.Contracts.ArtistDashboardCounts),
    typeof(Concertable.B2B.Conversations.Contracts.Events.ConversationChanged),
    typeof(Concertable.B2B.Deal.Contracts.FlatFeeTerms),
    typeof(Concertable.B2B.Seed.Contracts.Specs.ArtistSeedSpec),
    typeof(Concertable.B2B.Tenant.Contracts.ActivityItemDto),
    typeof(Concertable.B2B.TestKit.B2BTestClient),
    typeof(Concertable.B2B.User.Contracts.UserDto),
    typeof(Concertable.B2B.Venue.Contracts.IVenueModule),
};

Console.WriteLine($"{Concertable.B2B.Hosting.B2BService.Name}:{Concertable.B2B.Hosting.B2BDatabase.Name}:{boundTypes.Length}");
'@)

    [System.IO.File]::WriteAllText($configPath, @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="b2b-candidates" value="$escapedPackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/Concertable/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="b2b-candidates">
$packagePatterns
    </packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    <packageSource key="github"><package pattern="Concertable.*" /></packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="Concertable" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
"@)

    $packagesPath = Join-Path $consumerDirectory 'packages'
    & dotnet restore $projectPath --configfile $configPath --packages $packagesPath --no-cache
    if ($LASTEXITCODE -ne 0) {
        throw "Clean package-consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build $projectPath --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Clean package-consumer build failed with exit code $LASTEXITCODE."
    }
}
finally {
    if ([System.IO.Directory]::Exists($consumerDirectory)) {
        [System.IO.Directory]::Delete($consumerDirectory, $true)
    }
}
