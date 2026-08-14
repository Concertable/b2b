namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueOrgIdentityLookup
{
    Task<VenueOrgIdentity?> GetByTenantIdAsync(Guid tenantId);
}
