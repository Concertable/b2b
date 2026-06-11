using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The public marketplace surface over opportunities — anonymous browse, no private contents.
/// Reads run with the "Tenant" filter lifted (see the implementation's <c>AllTenants</c> root);
/// management reads live on <see cref="IOpportunityRepository"/>, which is tenant-scoped.
/// </summary>
internal interface IPublicOpportunityRepository
{
    Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams);
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
}
