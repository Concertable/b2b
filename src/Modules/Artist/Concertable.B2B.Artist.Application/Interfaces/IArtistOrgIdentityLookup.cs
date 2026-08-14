namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistOrgIdentityLookup
{
    Task<ArtistOrgIdentity?> GetByTenantIdAsync(Guid tenantId);
}
