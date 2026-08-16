using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Application.Requests;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistService
{
    Task<Result<ArtistDetails, ArtistError>> GetDetailsByIdAsync(int id);
    Task<Result<ArtistDetails, ArtistError>> GetDetailsForCurrentUserAsync();
    Task<Result<ArtistDetails, CreateArtistError>> CreateAsync(CreateArtistRequest request);
    Task<Result<ArtistDetails, UpdateArtistError>> UpdateAsync(int id, UpdateArtistRequest request);
    Task<Option<int>> GetIdForCurrentTenantAsync();
    Task<bool> OwnsArtistAsync(int artistId);

    Task<Option<ArtistSummary>> GetSummaryAsync(int id);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
