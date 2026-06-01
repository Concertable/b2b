using Concertable.B2B.Artist.Application.DTOs;
using Concertable.DataAccess.Application;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistRepository : IRepository<ArtistEntity>
{
    Task<int?> GetIdByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetFullByIdAsync(int id);
    Task<ArtistSummaryDto?> GetSummaryAsync(int id);
    Task<ArtistDto?> GetDtoByIdAsync(int id);
    Task<ArtistDto?> GetDtoByUserIdAsync(Guid userId);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
