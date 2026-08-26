using Concertable.B2B.Concert.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Settlement;

public sealed class SettlementAmountResolverTests : IDisposable
{
    private readonly Mock<IConcertRepository> concertRepository;
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    private readonly ISettlementAmountResolver resolver;

    public SettlementAmountResolverTests()
    {
        this.concertRepository = new Mock<IConcertRepository>(MockBehavior.Strict);
        this.concertRepository
            .Setup(repository => repository.GetTotalRevenueByConcertIdAsync(3))
            .ReturnsAsync((decimal?)400m);
        this.concertRepository
            .Setup(repository => repository.GetTotalRevenueByConcertIdAsync(4))
            .ReturnsAsync((decimal?)220m);

        var services = new ServiceCollection();
        services.AddScoped(_ => this.concertRepository.Object);
        services.AddConcertDealStrategies();
        this.provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        this.scope = this.provider.CreateScope();
        this.resolver = this.scope.ServiceProvider.GetRequiredService<ISettlementAmountResolver>();
    }

    [Fact]
    public async Task ResolveGrossAsync_FlatFee_ReturnsFixedFee()
    {
        var deal = new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 200m };

        var result = await this.resolver.ResolveGrossAsync(1, deal);

        Assert.Equal(200m, result.Amount);
    }

    [Fact]
    public async Task ResolveGrossAsync_DoorSplit_ReturnsPercentageOfLoadedRevenue()
    {
        var deal = new DoorSplitDealDto { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70m };

        var result = await this.resolver.ResolveGrossAsync(3, deal);

        Assert.Equal(280m, result.Amount);
        this.concertRepository.Verify(
            repository => repository.GetTotalRevenueByConcertIdAsync(3),
            Times.Once);
    }

    [Fact]
    public async Task ResolveGrossAsync_Versus_ReturnsGuaranteePlusPercentageOfLoadedRevenue()
    {
        var deal = new VersusDealDto
        {
            PaymentMethod = PaymentMethod.Cash,
            Guarantee = 100m,
            ArtistDoorPercent = 70m
        };

        var result = await this.resolver.ResolveGrossAsync(4, deal);

        Assert.Equal(254m, result.Amount);
        this.concertRepository.Verify(
            repository => repository.GetTotalRevenueByConcertIdAsync(4),
            Times.Once);
    }

    [Fact]
    public async Task ResolveGrossAsync_VenueHire_ReturnsHireFee()
    {
        var deal = new VenueHireDealDto { PaymentMethod = PaymentMethod.Cash, HireFee = 300m };

        var result = await this.resolver.ResolveGrossAsync(2, deal);

        Assert.Equal(300m, result.Amount);
    }

    public void Dispose()
    {
        this.scope.Dispose();
        this.provider.Dispose();
    }
}
