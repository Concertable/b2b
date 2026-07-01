using Concertable.Contracts;

namespace Concertable.B2B.Artist.Contracts;

public interface IArtistModule
{
    Task<int?> GetIdForCurrentTenantAsync();
    Task<ArtistSummary> GetSummaryAsync(int artistId);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId);
}
