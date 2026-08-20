namespace Concertable.B2B.DealDispatch.Generators.UnitTests;

public sealed class VariantFactoryGenerationTests
{
    [Fact]
    public void CompleteVariantCatalog_GeneratesAliasesAndRegistrations()
    {
        var result = GeneratorTestHarness.Run(FixtureSource.Infrastructure + FixtureSource.Variant);

        Assert.Empty(result.Errors);
        Assert.Contains("global::Fixture.Contracts.DoorSplitDeal =>", result.GeneratedSource);
        Assert.Contains("global::Fixture.Contracts.VenueHireDeal =>", result.GeneratedSource);
        Assert.Contains("AddAcceptHandlers", result.GeneratedSource);
    }

    [Fact]
    public void MissingVariantCase_ReportsCoverageDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Variant)
            .Replace(", typeof(VenueHireDeal)", "");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD008");
    }

    [Fact]
    public void DuplicateVariantCase_ReportsCoverageDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Variant)
            .Replace(
                "[DealVariantCases(typeof(FlatFeeDeal))]",
                "[DealVariantCases(typeof(FlatFeeDeal), typeof(DoorSplitDeal))]");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD008");
    }

    [Fact]
    public void ImplementationOutsideWrapper_ReportsMembershipDiagnostic()
    {
        var source = (FixtureSource.Infrastructure + FixtureSource.Variant)
            .Replace(
                "internal AcceptHandler(PaidAcceptHandler handler) { }",
                "internal AcceptHandler(string value) { }");

        var result = GeneratorTestHarness.Run(source);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "DDD009");
    }
}
