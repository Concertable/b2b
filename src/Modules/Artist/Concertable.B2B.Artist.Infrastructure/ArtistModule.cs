using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure;

internal sealed class ArtistModule : IArtistModule
{
    private readonly IArtistService artistService;
    private readonly IArtistRepository repository;

    public ArtistModule(IArtistService artistService, IArtistRepository repository)
    {
        this.artistService = artistService;
        this.repository = repository;
    }

    public Task<int?> GetIdByUserIdAsync(Guid userId) =>
        repository.GetIdByUserIdAsync(userId);

    public Task<ArtistSummary> GetSummaryAsync(int artistId) =>
        artistService.GetSummaryAsync(artistId);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId) =>
        artistService.GetGenresAsync(artistId);
}
