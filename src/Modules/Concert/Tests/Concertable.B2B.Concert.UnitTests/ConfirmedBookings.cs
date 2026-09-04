using Concertable.B2B.Booking.Contracts;
using PaymentCommitment = Concertable.B2B.Booking.Contracts.PaymentCommitment;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.UnitTests;

internal static class ConfirmedBookings
{
    public static ConfirmedBookingSnapshot FlatFee(decimal fee = 500m, params Genre[] genres) =>
        Snapshot(new ConfirmedBookingTerms.FlatFee(fee), "escrow-hold", genres);

    public static ConfirmedBookingSnapshot VenueHire(decimal hireFee = 250m, params Genre[] genres) =>
        Snapshot(new ConfirmedBookingTerms.VenueHire(hireFee), "method-setup", genres);

    public static ConfirmedBookingSnapshot DoorSplit(decimal artistDoorPercent = 50m, params Genre[] genres) =>
        Snapshot(new ConfirmedBookingTerms.DoorSplit(artistDoorPercent), "method-verification", genres);

    public static ConfirmedBookingSnapshot Versus(
        decimal guarantee = 100m,
        decimal artistDoorPercent = 50m,
        params Genre[] genres) =>
        Snapshot(new ConfirmedBookingTerms.Versus(guarantee, artistDoorPercent), "method-verification", genres);

    private static ConfirmedBookingSnapshot Snapshot(
        ConfirmedBookingTerms terms,
        string operationType,
        Genre[] genres) =>
        new(
            1,
            2,
            3,
            4,
            5,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc),
            genres.Length > 0 ? genres : [Genre.Rock],
            new PaymentCommitment(operationType, "app:2"),
            terms);
}
