using Reunion;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Application.Updaters;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Enums;

namespace Concertable.B2B.Deal.UnitTests.Updaters;

public sealed class DealUpdaterTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Update_MatchingDealType_WritesTheDtoOntoTheEntity(DealEntity entity, DealDto deal)
    {
        var result = DealUpdater.Update(entity, deal);

        Assert.True(result.IsSuccess);
        Assert.Equal(deal, DealMapper.ToDto(entity));
    }

    [Fact]
    public void Update_EveryDealType_HasAnArm()
    {
        var covered = Cases.Cast<object[]>().Select(row => ((DealDto)row[1]).DealType).ToHashSet();

        Assert.Equal(Enum.GetValues<DealType>().ToHashSet(), covered);
    }

    [Fact]
    public void Update_MismatchedDealType_ReturnsFailureAndLeavesTheEntityUnchanged()
    {
        var entity = Build(FlatFeeDealEntity.Create(100, PaymentMethod.Cash));

        var result = DealUpdater.Update(entity, new DoorSplitDealDto { ArtistDoorPercent = 50 });

        Assert.True(result.IsFailure);
        Assert.Equal(100, entity.Fee);
        Assert.Equal(PaymentMethod.Cash, entity.PaymentMethod);
    }

    [Fact]
    public void Update_InvalidTerms_ReturnsFailureAndLeavesTheEntityUnchanged()
    {
        var entity = Build(FlatFeeDealEntity.Create(100, PaymentMethod.Cash));

        var result = DealUpdater.Update(entity, new FlatFeeDealDto { Fee = 0, PaymentMethod = PaymentMethod.Transfer });

        Assert.True(result.IsFailure);
        Assert.Equal(100, entity.Fee);
        Assert.Equal(PaymentMethod.Cash, entity.PaymentMethod);
    }

    public static TheoryData<DealEntity, DealDto> Cases { get; } = new()
    {
        {
            Build(FlatFeeDealEntity.Create(100, PaymentMethod.Cash)),
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 250 }
        },
        {
            Build(DoorSplitDealEntity.Create(40, PaymentMethod.Cash)),
            new DoorSplitDealDto { PaymentMethod = PaymentMethod.Transfer, ArtistDoorPercent = 65 }
        },
        {
            Build(VersusDealEntity.Create(100, 40, PaymentMethod.Cash)),
            new VersusDealDto { PaymentMethod = PaymentMethod.Transfer, Guarantee = 300, ArtistDoorPercent = 75 }
        },
        {
            Build(VenueHireDealEntity.Create(100, PaymentMethod.Cash)),
            new VenueHireDealDto { PaymentMethod = PaymentMethod.Transfer, HireFee = 500 }
        }
    };

    private static TEntity Build<TEntity>(Result<TEntity, ValidationErrors> result)
        where TEntity : DealEntity =>
        result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException("The test fixture built an invalid deal.");
}
