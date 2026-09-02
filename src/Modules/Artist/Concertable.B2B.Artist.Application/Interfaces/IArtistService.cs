using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Application.Requests;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistService
{
    Task<Option<ArtistDetails>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default);
    Task<Option<ArtistDetails>> GetDetailsAsync(
        CancellationToken ct = default);
    Task<Result<ArtistDetails, CreateArtistError>> CreateAsync(
        CreateArtistRequest request,
        CancellationToken ct = default);
    Task<Result<ArtistDetails, UpdateArtistError>> UpdateAsync(
        UpdateArtistRequest request,
        CancellationToken ct = default);
    Task<bool> OwnsArtistAsync(int artistId, CancellationToken ct = default);

    Task<Option<ArtistSummary>> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ArtistSummary>> GetSummariesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id, CancellationToken ct = default);
    Task<Option<ArtistDto>> GetProfileAsync(int id, CancellationToken ct = default);
    Task<Option<ArtistDto>> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<Option<TenantContact>> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
