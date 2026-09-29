namespace Concertable.B2B.Venue.Contracts;

public interface IVenuePrivilegedRepository
{
    Task<VenueProfile?> GetByIdAsync(int venueId, CancellationToken ct = default);
}
