namespace Concertable.B2B.Application.Domain.Entities;

public sealed class ConcertAvailabilityEntity
{
    public int ConcertId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public DateTime StartDate { get; private set; }

    private ConcertAvailabilityEntity() { }

    public static ConcertAvailabilityEntity Create(
        int concertId,
        int opportunityId,
        int artistId,
        int venueId,
        DateTime startDate) => new()
    {
        ConcertId = concertId,
        OpportunityId = opportunityId,
        ArtistId = artistId,
        VenueId = venueId,
        StartDate = startDate
    };
}
