using Concertable.B2B.Infrastructure.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.UnitTests.Strategies;

public sealed class DealUnionBuilderTests
{
    [Fact]
    public void Build_CompleteCoverage_RegistersDealUnionFactory()
    {
        var services = new ServiceCollection();
        var builder = new DealUnionBuilder<TestUnion>(services);
        builder.Case<IFirstCase>(first => new TestUnion.First(first))
            .Use<FirstCase>(DealType.FlatFee)
            .Use<OtherFirstCase>(DealType.VenueHire);
        builder.Case<ISecondCase>(second => new TestUnion.Second(second))
            .Use<SecondCase>(DealType.DoorSplit, DealType.Versus);
        builder.Build();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDealUnionFactory<TestUnion>>();

        Assert.IsType<FirstCase>(Assert.IsType<TestUnion.First>(factory.Create(DealType.FlatFee)).DealStrategy);
        Assert.IsType<OtherFirstCase>(
            Assert.IsType<TestUnion.First>(factory.Create(DealType.VenueHire)).DealStrategy);
        Assert.IsType<TestUnion.Second>(factory.Create(DealType.DoorSplit));
        Assert.IsType<TestUnion.Second>(factory.Create(DealType.Versus));
    }

    [Fact]
    public void Build_MissingDealType_ThrowsBeforeRegistration()
    {
        var services = new ServiceCollection();
        var builder = new DealUnionBuilder<TestUnion>(services);
        builder.Case<IFirstCase>(first => new TestUnion.First(first))
            .Use<FirstCase>(DealType.FlatFee, DealType.DoorSplit, DealType.Versus);

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Contains("Missing: VenueHire", exception.Message);
        Assert.Empty(services);
    }

    private interface IFirstCase : IDealStrategy;

    private interface ISecondCase : IDealStrategy;

    private sealed class FirstCase : IFirstCase;

    private sealed class OtherFirstCase : IFirstCase;

    private sealed class SecondCase : ISecondCase;

    private abstract record TestUnion
    {
        public sealed record First(IFirstCase DealStrategy) : TestUnion;

        public sealed record Second(ISecondCase DealStrategy) : TestUnion;
    }
}
