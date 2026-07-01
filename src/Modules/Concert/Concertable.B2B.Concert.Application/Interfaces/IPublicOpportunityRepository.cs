using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The public marketplace surface over opportunities — anonymous browse, no private contents.
/// Reads run on the read-only <c>PublicConcertDbContext</c>, which composes no tenant filters,
/// so the open/active check sees <b>all</b> parties' applications — an opportunity already booked
/// by another tenant correctly stops showing as open. Management reads live on
/// <see cref="IOpportunityRepository"/>, which is tenant-scoped.
/// </summary>
internal interface IPublicOpportunityRepository
{
    Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams);
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
}
