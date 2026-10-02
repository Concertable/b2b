namespace Concertable.B2B.Booking.Domain.Lifecycle;

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
