using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReadRepository
{
    Task<ArtistSummary?> GetSummaryAsync(int id);
    Task<ArtistDetails?> GetDetailsByIdAsync(int id);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
