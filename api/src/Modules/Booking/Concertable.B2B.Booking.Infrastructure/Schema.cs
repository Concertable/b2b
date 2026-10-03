namespace Concertable.B2B.Booking.Infrastructure;

internal static class Schema
{
    public const string Name = "booking";

    public static class Tables
    {
        public const string Bookings = "Bookings";
        public const string Contracts = "Contracts";
        public const string BookingAccessGrants = "BookingAccessGrants";
        public const string ContractAccessGrants = "ContractAccessGrants";
    }
}
