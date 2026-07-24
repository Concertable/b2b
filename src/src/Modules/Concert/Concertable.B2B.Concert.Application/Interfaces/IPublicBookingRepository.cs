namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The public (unfiltered) stance over bookings. Reads run on the read-only
/// <c>PublicConcertDbContext</c>, which composes no tenant filters, so existence is judged across
/// <b>all</b> tenants. Management reads live on <see cref="IBookingRepository"/>, which is tenant-scoped.
/// </summary>
internal interface IPublicBookingRepository
{
    Task<bool> ExistsAsync(int bookingId);
}
