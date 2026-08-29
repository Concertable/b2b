using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Deal.Infrastructure.Extensions;
using Concertable.B2B.Deal.Infrastructure.Services.Updaters;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyArchitectureTests
{
    [Fact]
    public void DealDtoEntityEnumJsonAndTypeScriptCatalogs_Agree()
    {
        var dtoCases = DirectCases(typeof(DealDto), "DealDto");
        var entityCases = DirectCases(typeof(DealEntity), "DealEntity");
        var enumCases = Enum.GetNames<DealType>().Order(StringComparer.Ordinal).ToArray();
        var jsonCases = typeof(DealDto)
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .Cast<JsonDerivedTypeAttribute>()
            .Select(attribute => new
            {
                Stem = TrimSuffix(attribute.DerivedType.Name, "DealDto"),
                Discriminator = Assert.IsType<string>(attribute.TypeDiscriminator)
            })
            .OrderBy(item => item.Stem, StringComparer.Ordinal)
            .ToArray();
        var typeScript = File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(FindApiRoot())!,
            "app",
            "web",
            "b2b",
            "shared",
            "src",
            "features",
            "deals",
            "types.ts"));

        Assert.Equal(dtoCases, entityCases);
        Assert.Equal(dtoCases, enumCases);
        Assert.Equal(dtoCases, jsonCases.Select(item => item.Stem));
        foreach (var item in jsonCases)
            Assert.Contains($"$type: \"{item.Discriminator}\"", typeScript, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyedRegistrations_CoverEveryStrategyFamilyAndDealTypeExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddDealStrategies();
        var expected = new Dictionary<(Type Family, DealType Case), Type>
        {
            [(typeof(IDealMapper), DealType.FlatFee)] = typeof(FlatFeeDealMapper),
            [(typeof(IDealMapper), DealType.DoorSplit)] = typeof(DoorSplitDealMapper),
            [(typeof(IDealMapper), DealType.Versus)] = typeof(VersusDealMapper),
            [(typeof(IDealMapper), DealType.VenueHire)] = typeof(VenueHireDealMapper),
            [(typeof(IDealUpdater), DealType.FlatFee)] = typeof(FlatFeeDealUpdater),
            [(typeof(IDealUpdater), DealType.DoorSplit)] = typeof(DoorSplitDealUpdater),
            [(typeof(IDealUpdater), DealType.Versus)] = typeof(VersusDealUpdater),
            [(typeof(IDealUpdater), DealType.VenueHire)] = typeof(VenueHireDealUpdater)
        };
        var catalog = new[] { typeof(IDealMapper), typeof(IDealUpdater) }
            .SelectMany(family => Enum.GetValues<DealType>().Select(dealType => (family, dealType)))
            .ToHashSet();
        var actual = services
            .Where(descriptor => descriptor.IsKeyedService)
            .Where(descriptor => descriptor.ServiceType == typeof(IDealMapper)
                || descriptor.ServiceType == typeof(IDealUpdater))
            .ToArray();

        Assert.True(catalog.SetEquals(expected.Keys));
        Assert.Equal(expected.Count, actual.Length);
        foreach (var descriptor in actual)
        {
            var key = (descriptor.ServiceType, Assert.IsType<DealType>(descriptor.ServiceKey));
            Assert.Equal(expected[key], descriptor.KeyedImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }
    }

    [Fact]
    public void DealTypeFrozenDictionaries_AreAbsent()
    {
        var violations = EnumerateProductionFiles()
            .Where(path => File.ReadAllText(path).Contains("FrozenDictionary<DealType", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void KeyedServiceProvider_AppearsOnlyInModuleFactoriesAndCompositionRoots()
    {
        var violations = EnumerateProductionFiles()
            .Where(path => File.ReadAllText(path).Contains("IKeyedServiceProvider", StringComparison.Ordinal))
            .Where(path => !IsAllowlisted(path, KeyedProviderFiles))
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(KeyedProviderFiles))]
    public void KeyedProviderAllowlist_StillUsesKeyedServiceProvider(string relativePath)
    {
        var source = File.ReadAllText(FindSourceFile(relativePath));

        Assert.Contains("IKeyedServiceProvider", source, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyedServiceLookup_AppearsOnlyInModuleFactories()
    {
        var violations = EnumerateProductionFiles()
            .Where(path => File.ReadAllText(path).Contains("GetRequiredKeyedService", StringComparison.Ordinal))
            .Where(path => !IsAllowlisted(path, StrategyFactoryFiles))
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(StrategyFactoryFiles))]
    public void StrategyFactoryAllowlist_StillOwnsKeyedServiceLookup(string relativePath)
    {
        var source = File.ReadAllText(FindSourceFile(relativePath));

        Assert.Contains("GetRequiredKeyedService", source, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(RequiredCoverageDeclarations))]
    public void RequiredStrategyFamily_DeclaresExactCoverage(string relativePath, string declaration)
    {
        var source = File.ReadAllText(FindSourceFile(relativePath));

        Assert.Contains(declaration, source, StringComparison.Ordinal);
    }

    public static TheoryData<string> KeyedProviderFiles { get; } = new()
    {
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "Concertable.B2B/src/Concertable.B2B.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
        "Concertable.B2B/src/Concertable.B2B.Infrastructure/Services/Strategies/DealTypeStrategyFactory.cs"
    };

    public static TheoryData<string> StrategyFactoryFiles { get; } = new()
    {
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "Concertable.B2B/src/Concertable.B2B.Infrastructure/Services/Strategies/DealTypeStrategyFactory.cs"
    };

    public static TheoryData<string, string> RequiredCoverageDeclarations { get; } = new()
    {
        {
            "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IDealMapper>();"
        },
        {
            "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IDealUpdater>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IDealPayeeResolver>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<ISettlementAmountResolver>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<ICancelStep>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<ICompleteStep>();"
        }
    };

    private static IEnumerable<string> EnumerateProductionFiles()
    {
        var apiRoot = FindApiRoot();
        var moduleRoots = new[]
        {
            Path.Combine(apiRoot, "Concertable.B2B", "src", "Modules", "Deal"),
            Path.Combine(apiRoot, "Concertable.B2B", "src", "Modules", "Concert")
        };

        return moduleRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedOrTestPath(path));
    }

    private static bool IsAllowlisted(string path, TheoryData<string> allowlist)
    {
        var normalized = path.Replace('\\', '/');
        return allowlist
            .Cast<object[]>()
            .Any(row => normalized.EndsWith((string)row[0], StringComparison.Ordinal));
    }

    private static bool IsGeneratedOrTestPath(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}Tests{separator}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSourceFile(string relativePath) =>
        Path.Combine(FindApiRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string[] DirectCases(Type root, string suffix) =>
        root.Assembly.GetTypes()
            .Where(type => type.BaseType == root && type.IsSealed)
            .Select(type => TrimSuffix(type.Name, suffix))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string TrimSuffix(string value, string suffix) =>
        value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;

    private static string FindApiRoot([CallerFilePath] string sourcePath = "")
    {
        var starts = new[]
        {
            Path.GetDirectoryName(sourcePath)!,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var start in starts)
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var apiRoot = Path.Combine(directory.FullName, "api");
                if (File.Exists(Path.Combine(apiRoot, "Concertable.slnx")))
                    return apiRoot;

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate api/Concertable.slnx.");
    }
}
