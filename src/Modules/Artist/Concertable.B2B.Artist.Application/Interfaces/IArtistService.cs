using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Application.Requests;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistService
{
    Task<Option<ArtistDetails>> GetDetailsByIdAsync(int id);
    Task<Option<ArtistDetails>> GetDetailsForCurrentUserAsync();
    Task<Result<ArtistDetails, CreateArtistError>> CreateAsync(CreateArtistRequest request);
    Task<Result<ArtistDetails, UpdateArtistError>> UpdateAsync(int id, UpdateArtistRequest request);
    Task<Option<int>> GetIdForCurrentUserAsync();
    Task<bool> OwnsArtistAsync(int artistId);

    Task<Option<ArtistSummary>> GetSummaryAsync(int id);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
    Task<Option<ArtistOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId);
}
