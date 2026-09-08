using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;

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
            FindRepositoryRoot(),
            "app",
            "web",
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

    public static TheoryData<string> KeyedProviderFiles { get; } = new()
    {
        "api/src/Concertable.B2B.Infrastructure/Extensions/ServiceCollectionExtensions.cs",
        "api/src/Concertable.B2B.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "api/src/Concertable.B2B.Infrastructure/Services/Strategies/DealUnionFactory.cs"
    };

    public static TheoryData<string> StrategyFactoryFiles { get; } = new()
    {
        "api/src/Concertable.B2B.Infrastructure/Services/Strategies/DealStrategyFactory.cs",
        "api/src/Concertable.B2B.Infrastructure/Services/Strategies/DealUnionFactory.cs"
    };

    private static IEnumerable<string> EnumerateProductionFiles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var moduleRoots = new[]
        {
            Path.Combine(repositoryRoot, "api", "src", "Modules", "Deal"),
            Path.Combine(repositoryRoot, "api", "src", "Modules", "Concert")
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
        Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

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

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
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
                if (File.Exists(Path.Combine(directory.FullName, "Concertable.B2B.slnx")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate Concertable.B2B.slnx.");
    }
}
