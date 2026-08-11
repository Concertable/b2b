using System.Runtime.CompilerServices;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealStrategyArchitectureTests
{
    [Fact]
    public void DealTypeFrozenDictionaries_AppearOnlyInWorkflowRegistries()
    {
        var violations = EnumerateProductionFiles()
            .Where(path => File.ReadAllText(path).Contains("FrozenDictionary<DealType", StringComparison.Ordinal))
            .Where(path => !IsAllowlisted(path, WorkflowRegistryFiles))
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(WorkflowRegistryFiles))]
    public void WorkflowRegistryAllowlist_StillContainsDealTypeFrozenDictionary(string relativePath)
    {
        var source = File.ReadAllText(FindSourceFile(relativePath));

        Assert.Contains("FrozenDictionary<DealType", source, StringComparison.Ordinal);
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

    public static TheoryData<string> WorkflowRegistryFiles { get; } = new()
    {
        "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Workflow/ConcertStateMachineRegistry.cs",
        "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Workflow/ConcertWorkflowCapabilityRegistry.cs"
    };

    public static TheoryData<string> KeyedProviderFiles { get; } = new()
    {
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
        "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Strategies/ConcertDealStrategyFactory.cs"
    };

    public static TheoryData<string> StrategyFactoryFiles { get; } = new()
    {
        "Concertable.B2B/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Strategies/ConcertDealStrategyFactory.cs"
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
            "strategies.RequireAll<IDealTerms>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IDealPayeeResolver>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IPaymentAmountMapper>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<ISettlementAmountResolver>();"
        },
        {
            "Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
            "strategies.RequireAll<IConcertWorkflow>();"
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
