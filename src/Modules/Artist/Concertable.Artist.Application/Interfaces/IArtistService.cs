using Concertable.Artist.Application.DTOs;
using Concertable.Artist.Application.Requests;

namespace Concertable.Artist.Application.Interfaces;

internal interface IArtistService
{
    Task<ArtistDto> GetDetailsByIdAsync(int id);
    Task<ArtistDto?> GetDetailsForCurrentUserAsync();
    Task<ArtistDto> CreateAsync(CreateArtistRequest request);
    Task<ArtistDto> UpdateAsync(int id, UpdateArtistRequest request);
    Task<int> GetIdForCurrentUserAsync();
    Task<bool> OwnsArtistAsync(int artistId);
}
