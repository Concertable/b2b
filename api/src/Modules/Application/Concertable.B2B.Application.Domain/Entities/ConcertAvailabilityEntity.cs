using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Domain.Entities;

public sealed class ConcertAvailabilityEntity : IVenueArtistTenantScoped
{
    public int ConcertId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public DateTime StartDate { get; private set; }

    private ConcertAvailabilityEntity() { }

    public static ConcertAvailabilityEntity Create(
        int concertId,
        int opportunityId,
        int artistId,
        int venueId,
        Guid venueTenantId,
        Guid artistTenantId,
        DateTime startDate) => new()
    {
        ConcertId = concertId,
        OpportunityId = opportunityId,
        ArtistId = artistId,
        VenueId = venueId,
        VenueTenantId = venueTenantId,
        ArtistTenantId = artistTenantId,
        StartDate = startDate
    };
}
