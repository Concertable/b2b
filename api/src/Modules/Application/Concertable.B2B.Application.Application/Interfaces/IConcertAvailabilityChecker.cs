namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IConcertAvailabilityChecker
{
    Task<bool> OpportunityHasConcertAsync(int opportunityId, CancellationToken ct = default);
    Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date, CancellationToken ct = default);
    Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date, CancellationToken ct = default);
}
