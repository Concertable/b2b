using Concertable.Contracts.Enums;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Artist.Contracts;

public interface IArtistModule
{
    Task<Option<ArtistSummary>> GetSummaryAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlyList<ArtistSummary>> GetSummariesAsync(
        IReadOnlyCollection<int> artistIds,
        CancellationToken ct = default);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId, CancellationToken ct = default);
    Task<Option<ArtistProfile>> GetProfileAsync(int artistId, CancellationToken ct = default);
    Task<Option<ArtistProfile>> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<ReviewSummary> GetReviewSummaryAsync(int artistId, CancellationToken ct = default);
    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
