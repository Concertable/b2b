using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Contracts.Enums;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Concert.UnitTests;

internal static class ConfirmedBookings
{
    public static readonly Guid VenueTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ArtistTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly DateTime StartsAtUtc = new(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime EndsAtUtc = new(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc);

    public static ConfirmedBookingSnapshot FlatFee(decimal fee = 500m, params Genre[] genres) =>
        Snapshot(new ConfirmedBookingTerms.FlatFee(fee), PaymentOperationReferences.EscrowHold(2), genres);

    public static ConfirmedBookingSnapshot VenueHire(decimal hireFee = 250m, params Genre[] genres) =>
        Snapshot(
            new ConfirmedBookingTerms.VenueHire(hireFee),
            PaymentOperationReferences.MethodSetup(3, ArtistTenantId),
            genres);

    public static ConfirmedBookingSnapshot DoorSplit(decimal artistDoorPercent = 50m, params Genre[] genres) =>
        Snapshot(
            new ConfirmedBookingTerms.DoorSplit(artistDoorPercent),
            PaymentOperationReferences.MethodVerification(2),
            genres);

    public static ConfirmedBookingSnapshot Versus(
        decimal guarantee = 100m,
        decimal artistDoorPercent = 50m,
        params Genre[] genres) =>
        Snapshot(
            new ConfirmedBookingTerms.Versus(guarantee, artistDoorPercent),
            PaymentOperationReferences.MethodVerification(2),
            genres);

    private static ConfirmedBookingSnapshot Snapshot(
        ConfirmedBookingTerms terms,
        PaymentOperationReference commitment,
        Genre[] genres) =>
        new(
            1,
            2,
            3,
            4,
            5,
            VenueTenantId,
            ArtistTenantId,
            StartsAtUtc,
            EndsAtUtc,
            genres.Length > 0 ? genres : [Genre.Rock],
            commitment,
            terms);
}
