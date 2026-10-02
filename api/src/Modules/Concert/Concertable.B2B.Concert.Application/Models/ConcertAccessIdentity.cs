namespace Concertable.B2B.Concert.Application.Models;

internal sealed record ConcertAccessIdentity(
    int ConcertId, Guid VenueTenantId, Guid ArtistTenantId, long AccessVersion);
