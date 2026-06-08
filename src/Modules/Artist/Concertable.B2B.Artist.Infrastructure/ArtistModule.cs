using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure;

internal sealed class ArtistModule : IArtistModule
{
    private readonly IArtistRepository repo;

    public ArtistModule(IArtistRepository repo)
    {
        this.repo = repo;
    }

    public Task<int?> GetIdByUserIdAsync(Guid userId) =>
        repo.GetIdByUserIdAsync(userId);

    public Task<ArtistSummary?> GetSummaryAsync(int artistId) =>
        repo.GetSummaryAsync(artistId);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId) =>
        repo.GetGenresAsync(artistId);
}
