namespace Concertable.B2B.Venue.Contracts;

public interface IVenueCommandFacts
{
    Task<VenueProfile?> GetByIdAsync(int venueId, CancellationToken ct = default);
}
