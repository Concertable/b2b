namespace Concertable.B2B.Venue.Contracts;

public interface IVenuePrivilegedReadRepository
{
    Task<VenueProfile?> GetByIdAsync(int venueId, CancellationToken ct = default);
}
