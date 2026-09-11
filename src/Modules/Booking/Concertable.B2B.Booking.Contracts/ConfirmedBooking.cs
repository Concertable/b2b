using Concertable.Contracts.Enums;
using Concertable.Payment.Contracts;
using Dunet;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Booking.Contracts;

public sealed record ConfirmedBookingSnapshot(
    int BookingId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    PaymentOperationReference Commitment,
    ConfirmedBookingTerms Terms);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConfirmedBookingTerms.FlatFee), "flatFee")]
[JsonDerivedType(typeof(ConfirmedBookingTerms.VenueHire), "venueHire")]
[JsonDerivedType(typeof(ConfirmedBookingTerms.DoorSplit), "doorSplit")]
[JsonDerivedType(typeof(ConfirmedBookingTerms.Versus), "versus")]
[Union(EnableImplicitConversions = false)]
public abstract partial record ConfirmedBookingTerms
{
    public partial record FlatFee(decimal Fee);

    public partial record VenueHire(decimal HireFee);

    public partial record DoorSplit(decimal ArtistDoorPercent);

    public partial record Versus(
        decimal Guarantee,
        decimal ArtistDoorPercent);
}
