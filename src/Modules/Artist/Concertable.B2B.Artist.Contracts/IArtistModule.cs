using Concertable.Contracts.Enums;
using Reunion;

namespace Concertable.B2B.Artist.Contracts;

public interface IArtistModule
{
    Task<Option<ArtistSummary>> GetSummaryAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId, CancellationToken ct = default);

    /// <summary>The artist's display name and business email for a given tenant — <see cref="Option{T}.None"/>
    /// when the tenant owns no artist. Used cross-module by admin listing and notification (verification review).</summary>
    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
