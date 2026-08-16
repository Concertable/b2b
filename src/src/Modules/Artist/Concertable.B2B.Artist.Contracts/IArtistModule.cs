using Concertable.Contracts.Enums;
using Reunion;

namespace Concertable.B2B.Artist.Contracts;

public interface IArtistModule
{
    Task<Option<int>> GetIdForCurrentTenantAsync();
    Task<Option<ArtistSummary>> GetSummaryAsync(int artistId);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId);
}
