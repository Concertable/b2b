using System.Text.Json;
using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class ConfirmedBookingTermsSerializationTests
{
    public static TheoryData<ConfirmedBookingTerms, string> Terms => new()
    {
        { new ConfirmedBookingTerms.FlatFee(500m), "flatFee" },
        { new ConfirmedBookingTerms.VenueHire(250m), "venueHire" },
        { new ConfirmedBookingTerms.DoorSplit(70m), "doorSplit" },
        { new ConfirmedBookingTerms.Versus(100m, 70m), "versus" },
    };

    [Theory]
    [MemberData(nameof(Terms))]
    public void Json_RoundTripsConcreteTermsThroughAbstractContract(
        ConfirmedBookingTerms terms,
        string discriminator)
    {
        var json = JsonSerializer.Serialize(new TermsEnvelope(terms));

        var result = JsonSerializer.Deserialize<TermsEnvelope>(json);

        Assert.Contains($"\"$type\":\"{discriminator}\"", json);
        Assert.Equal(terms, result?.Terms);
    }

    private sealed record TermsEnvelope(ConfirmedBookingTerms Terms);
}
