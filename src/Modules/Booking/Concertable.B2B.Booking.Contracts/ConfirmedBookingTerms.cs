namespace Concertable.B2B.Booking.Contracts;

public abstract record ConfirmedBookingTerms;

public sealed record FlatFeeBookingTerms(decimal Fee) : ConfirmedBookingTerms;

public sealed record DoorSplitBookingTerms(
    decimal ArtistDoorPercent,
    string PaymentMethodId) : ConfirmedBookingTerms;

public sealed record VersusBookingTerms(
    decimal Guarantee,
    decimal ArtistDoorPercent,
    string PaymentMethodId) : ConfirmedBookingTerms;

public sealed record VenueHireBookingTerms(decimal HireFee) : ConfirmedBookingTerms;
