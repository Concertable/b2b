using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure;

internal sealed class ArtistModule : IArtistModule
{
    private readonly IArtistService artistService;

    public ArtistModule(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    public Task<Option<int>> GetIdForCurrentTenantAsync() =>
        artistService.GetIdForCurrentTenantAsync();

    public Task<Option<ArtistSummary>> GetSummaryAsync(int artistId) =>
        artistService.GetSummaryAsync(artistId);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int artistId) =>
        artistService.GetGenresAsync(artistId);
}
