using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure;

internal sealed class ArtistModule : IArtistModule
{
    private readonly IArtistRepository repo;
    private readonly IPublicArtistRepository publicRepo;

    public ArtistModule(IArtistRepository repo, IPublicArtistRepository publicRepo)
    {
        this.repo = repo;
        this.publicRepo = publicRepo;
    }

    public Task<int?> GetIdByUserIdAsync(Guid userId) =>
        repo.GetIdByUserIdAsync(userId);

    public Task<ArtistSummary?> GetSummaryAsync(int artistId) =>
        publicRepo.GetSummaryAsync(artistId);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId) =>
        publicRepo.GetGenresAsync(artistId);
}
