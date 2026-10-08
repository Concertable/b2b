namespace Concertable.B2B.Artist.Contracts;

public interface IArtistPrivilegedReadRepository
{
    Task<ArtistProfile?> GetByIdAsync(int artistId, CancellationToken ct = default);
    Task<ArtistProfile?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<ArtistSummary?> GetSummaryByIdAsync(int artistId, CancellationToken ct = default);
}
