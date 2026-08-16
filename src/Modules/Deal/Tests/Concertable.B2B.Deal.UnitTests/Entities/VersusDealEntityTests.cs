using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class VersusDealEntityTests
{
    [Fact]
    public void Create_NegativeGuarantee_ReturnsFailure()
    {
        var result = VersusDealEntity.Create(-1m, 50m, PaymentMethod.Cash);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ArtistDoorPercentOutsideRange_ReturnsFailure(decimal artistDoorPercent)
    {
        var result = VersusDealEntity.Create(100m, artistDoorPercent, PaymentMethod.Cash);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Update_ValidTerms_ReplacesEconomicInputs()
    {
        var creation = VersusDealEntity.Create(100m, 25m, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var update = deal.Update(200m, 75m, PaymentMethod.Transfer);

        Assert.True(update.IsSuccess);
        Assert.Equal(200m, deal.Guarantee);
        Assert.Equal(75m, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Transfer, deal.PaymentMethod);
    }
}
