using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.UnitTests.Entities;

public sealed class DealEntityValidationTests
{
    [Fact]
    public void Create_NonPositiveFlatFee_ReturnsValidationFailure()
    {
        var result = FlatFeeDealEntity.Create(0, PaymentMethod.Cash);

        Assert.True(result.TryGetError(out var errors));
        Assert.Contains(nameof(FlatFeeDealEntity.Fee), errors.Errors.Keys);
    }

    [Fact]
    public void Create_NonPositiveHireFee_ReturnsValidationFailure()
    {
        var result = VenueHireDealEntity.Create(0, PaymentMethod.Cash);

        Assert.True(result.TryGetError(out var errors));
        Assert.Contains(nameof(VenueHireDealEntity.HireFee), errors.Errors.Keys);
    }

    [Fact]
    public void Create_OutOfRangeDoorPercent_ReturnsValidationFailure()
    {
        var result = DoorSplitDealEntity.Create(101, PaymentMethod.Cash);

        Assert.True(result.TryGetError(out var errors));
        Assert.Contains(nameof(DoorSplitDealEntity.ArtistDoorPercent), errors.Errors.Keys);
    }

    [Fact]
    public void Create_InvalidVersusTerms_AccumulatesValidationFailures()
    {
        var result = VersusDealEntity.Create(-1, 101, PaymentMethod.Cash);

        Assert.True(result.TryGetError(out var errors));
        Assert.Contains(nameof(VersusDealEntity.Guarantee), errors.Errors.Keys);
        Assert.Contains(nameof(VersusDealEntity.ArtistDoorPercent), errors.Errors.Keys);
    }

    [Fact]
    public void Update_InvalidTerms_ReturnsFailureWithoutMutatingDeal()
    {
        var creation = VersusDealEntity.Create(100, 50, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var result = deal.Update(-1, 101, PaymentMethod.Transfer);

        Assert.True(result.IsFailure);
        Assert.Equal(100, deal.Guarantee);
        Assert.Equal(50, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Cash, deal.PaymentMethod);
    }

    [Fact]
    public void Update_NonPositiveFlatFee_ReturnsFailureWithoutMutatingDeal()
    {
        var creation = FlatFeeDealEntity.Create(100, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var result = deal.Update(0, PaymentMethod.Transfer);

        Assert.True(result.IsFailure);
        Assert.Equal(100, deal.Fee);
        Assert.Equal(PaymentMethod.Cash, deal.PaymentMethod);
    }

    [Fact]
    public void Update_NonPositiveHireFee_ReturnsFailureWithoutMutatingDeal()
    {
        var creation = VenueHireDealEntity.Create(100, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var result = deal.Update(0, PaymentMethod.Transfer);

        Assert.True(result.IsFailure);
        Assert.Equal(100, deal.HireFee);
        Assert.Equal(PaymentMethod.Cash, deal.PaymentMethod);
    }

    [Fact]
    public void Update_OutOfRangeDoorPercent_ReturnsFailureWithoutMutatingDeal()
    {
        var creation = DoorSplitDealEntity.Create(50, PaymentMethod.Cash);
        Assert.True(creation.TryGetValue(out var deal));

        var result = deal.Update(101, PaymentMethod.Transfer);

        Assert.True(result.IsFailure);
        Assert.Equal(50, deal.ArtistDoorPercent);
        Assert.Equal(PaymentMethod.Cash, deal.PaymentMethod);
    }
}
