using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Requests;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistService
{
    Task<ArtistDetails> GetDetailsByIdAsync(int id);
    Task<ArtistDetails?> GetDetailsForCurrentUserAsync();
    Task<ArtistDetails> CreateAsync(CreateArtistRequest request);
    Task<ArtistDetails> UpdateAsync(int id, UpdateArtistRequest request);
    Task<int> GetIdForCurrentUserAsync();
    Task<bool> OwnsArtistAsync(int artistId);
}
