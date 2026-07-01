namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Cross-tenant concert availability — "is this slot taken?" facts for apply/accept validation.
/// A clash is a clash regardless of whose concert causes it, so these see every tenant's concerts;
/// only booleans leave this abstraction.
/// </summary>
internal interface IConcertAvailability
{
    Task<bool> OpportunityHasConcertAsync(int opportunityId);
    Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date);
    Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date);
}
