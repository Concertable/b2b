using Reunion;

namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default);

    /// <summary>The venue's display name and business email for a given tenant — <see cref="Option{T}.None"/>
    /// when the tenant owns no venue. Used cross-module by admin listing and notification (verification review).</summary>
    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
