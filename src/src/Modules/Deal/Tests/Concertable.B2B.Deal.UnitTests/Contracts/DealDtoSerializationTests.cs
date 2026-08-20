using System.Text.Json;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Deal.UnitTests.Contracts;

public sealed class DealDtoSerializationTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void JsonRoundTrip_PreservesDiscriminatorAndConcreteCase(
        DealDto deal,
        string discriminator)
    {
        var json = JsonSerializer.Serialize(deal);
        var roundTrip = JsonSerializer.Deserialize<DealDto>(json);

        Assert.Contains($"\"$type\":\"{discriminator}\"", json, StringComparison.Ordinal);
        Assert.Equal(deal, roundTrip);
        Assert.IsType(deal.GetType(), roundTrip);
    }

    public static TheoryData<DealDto, string> Cases { get; } = new()
    {
        {
            new FlatFeeDealDto
            {
                Id = 1,
                PaymentMethod = PaymentMethod.Transfer,
                Fee = 500
            },
            "flatFee"
        },
        {
            new DoorSplitDealDto
            {
                Id = 2,
                PaymentMethod = PaymentMethod.Cash,
                ArtistDoorPercent = 70
            },
            "doorSplit"
        },
        {
            new VersusDealDto
            {
                Id = 3,
                PaymentMethod = PaymentMethod.Cash,
                Guarantee = 200,
                ArtistDoorPercent = 60
            },
            "versus"
        },
        {
            new VenueHireDealDto
            {
                Id = 4,
                PaymentMethod = PaymentMethod.Transfer,
                HireFee = 300
            },
            "venueHire"
        }
    };
}
