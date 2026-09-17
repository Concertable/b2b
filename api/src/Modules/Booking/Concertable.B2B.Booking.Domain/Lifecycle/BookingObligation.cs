namespace Concertable.B2B.Booking.Domain.Lifecycle;

/// <summary>Which bookings still owe money in this stage. Confirmed is the handoff to Concert, but that
/// handoff travels as an integration event, so a Confirmed booking is only settled here once Concert has
/// acknowledged it — until then Concert has no row and neither gate would see the obligation.
/// Deny-by-default: an unlisted state blocks erasure until it is deliberately classified here.</summary>
internal static class BookingObligation
{
    public static IReadOnlySet<BookingState> SettledStates { get; } = new HashSet<BookingState>
    {
        BookingState.Cancelled,
    };

    public static IReadOnlySet<BookingState> SettledOnceHandedOff { get; } = new HashSet<BookingState>
    {
        BookingState.Confirmed,
    };

    public static bool IsLive(BookingState state, DateTime? handedOffAtUtc) =>
        !SettledStates.Contains(state)
        && !(SettledOnceHandedOff.Contains(state) && handedOffAtUtc is not null);
}
