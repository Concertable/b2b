namespace Concertable.B2B.Venue.Infrastructure.Extensions;

internal static class DateTimeExtensions
{
    extension(DateTime value)
    {
        public DateTime StartOfMonth() =>
            new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);
    }
}
