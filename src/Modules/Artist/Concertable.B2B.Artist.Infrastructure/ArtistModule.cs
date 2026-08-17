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

    public Task<Option<ArtistSummary>> GetSummaryAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetSummaryAsync(artistId, ct);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetGenresAsync(artistId, ct);

    public Task<Option<ArtistProfile>> GetProfileAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetProfileAsync(artistId, ct);

    public Task<Option<ArtistProfile>> GetCurrentProfileAsync(CancellationToken ct = default) =>
        artistService.GetCurrentProfileAsync(ct);
}
