namespace Concertable.B2B.Application.Contracts;

public interface IApplicationAvailabilityProjection
{
    Task<bool> OpportunityHasConcertAsync(int opportunityId);
    Task<bool> ArtistHasConcertOnDateAsync(int artistId, DateTime date);
    Task<bool> VenueHasConcertOnDateAsync(int venueId, DateTime date);
}
