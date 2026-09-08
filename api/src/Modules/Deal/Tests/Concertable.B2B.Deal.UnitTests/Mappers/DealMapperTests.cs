using Reunion;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Enums;

namespace Concertable.B2B.Deal.UnitTests.Mappers;

public sealed class DealMapperTests
{
    [Theory]
    [MemberData(nameof(Entities))]
    public void ToDto_Entity_ProjectsTheMatchingArm(DealEntity entity, DealDto expected)
    {
        var dto = DealMapper.ToDto(entity);

        Assert.Equal(expected, dto);
    }

    [Fact]
    public void ToDto_EveryDealType_HasAMappedArm()
    {
        var mapped = Entities
            .Cast<object[]>()
            .Select(row => DealMapper.ToDto((DealEntity)row[0]).DealType)
            .ToHashSet();

        Assert.Equal(Enum.GetValues<DealType>().ToHashSet(), mapped);
    }

    [Fact]
    public void ToDtos_Entities_PreservesOrder()
    {
        var entities = Entities.Cast<object[]>().Select(row => (DealEntity)row[0]).ToList();

        var dtos = DealMapper.ToDtos(entities);

        Assert.Equal(entities.Select(entity => entity.DealType), dtos.Select(dto => dto.DealType));
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public void ToEntity_Dto_RoundTripsBackToTheSameDto(DealEntity entity, DealDto dto)
    {
        var result = dto.ToEntity();

        Assert.True(result.TryGetValue(out var rebuilt));
        Assert.Equal(dto with { Id = 0 }, DealMapper.ToDto(rebuilt));
        Assert.Equal(entity.DealType, rebuilt.DealType);
    }

    [Fact]
    public void ToEntity_InvalidTerms_ReturnsValidationFailure()
    {
        var result = new FlatFeeDealDto { Fee = 0, PaymentMethod = PaymentMethod.Cash }.ToEntity();

        Assert.True(result.IsFailure);
    }

    public static TheoryData<DealEntity, DealDto> Entities { get; } = new()
    {
        {
            Build(FlatFeeDealEntity.Create(100, PaymentMethod.Cash)),
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 100 }
        },
        {
            Build(DoorSplitDealEntity.Create(60, PaymentMethod.Transfer)),
            new DoorSplitDealDto { PaymentMethod = PaymentMethod.Transfer, ArtistDoorPercent = 60 }
        },
        {
            Build(VersusDealEntity.Create(250, 70, PaymentMethod.Cash)),
            new VersusDealDto { PaymentMethod = PaymentMethod.Cash, Guarantee = 250, ArtistDoorPercent = 70 }
        },
        {
            Build(VenueHireDealEntity.Create(400, PaymentMethod.Transfer)),
            new VenueHireDealDto { PaymentMethod = PaymentMethod.Transfer, HireFee = 400 }
        }
    };

    private static TEntity Build<TEntity>(Result<TEntity, ValidationErrors> result)
        where TEntity : DealEntity =>
        result.TryGetValue(out var entity)
            ? entity
            : throw new InvalidOperationException("The test fixture built an invalid deal.");
}
