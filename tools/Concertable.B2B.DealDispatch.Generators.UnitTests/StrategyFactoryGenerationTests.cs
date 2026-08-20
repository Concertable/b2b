namespace Concertable.B2B.DealDispatch.Generators.UnitTests;

public sealed class StrategyFactoryGenerationTests
{
    [Fact]
    public void CompleteFamily_GeneratesFactorySwitchesAndRegistrations()
    {
        var result = GeneratorTestHarness.Run(FixtureSource.Infrastructure + FixtureSource.Strategy);

        Assert.Empty(result.Errors);
        Assert.Contains("GeneratedDealStrategyFactory", result.GeneratedSource);
        Assert.Contains("global::Fixture.Contracts.FlatFeeDeal => flatFee", result.GeneratedSource);
        Assert.Contains("global::Fixture.Domain.DoorSplitDealEntity => doorSplit", result.GeneratedSource);
        Assert.Contains("AddDealStrategies", result.GeneratedSource);
    }

    [Fact]
    public void MissingFamilyCase_ReportsCoverageDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Strategy)
            .Replace(
                "internal sealed class DoorSplitDealMapper : IDealMapper;",
                "");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD005");
    }

    [Fact]
    public void DuplicateFamilyCase_ReportsCoverageDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Strategy)
            .Replace(
                "internal sealed class DoorSplitDealMapper : IDealMapper;",
                """
                internal sealed class DoorSplitDealMapper : IDealMapper;
                }
                namespace Fixture.Application.Duplicate
                {
                    using Fixture.Application;
                    internal sealed class DoorSplitDealMapper : IDealMapper;
                """);

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD005");
    }

    [Fact]
    public void MarkerMismatch_ReportsContractDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Strategy)
            .Replace(
                "typeof(IDealStrategy))]",
                "typeof(object))]");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD001");
    }

    [Fact]
    public void MissingRegistrationInvocation_ReportsDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Strategy)
            .Replace("services.AddDealStrategies();", "services;");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD007");
    }
}
