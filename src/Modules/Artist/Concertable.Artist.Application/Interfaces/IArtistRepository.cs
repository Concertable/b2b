using Concertable.DataAccess;
using Concertable.Artist.Application.DTOs;
using Concertable.Shared;

namespace Concertable.Artist.Application.Interfaces;

internal interface IArtistRepository : IIdRepository<ArtistEntity>
{
    Task<int?> GetIdByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetFullByIdAsync(int id);
    Task<ArtistSummaryDto?> GetSummaryAsync(int id);
    Task<ArtistDto?> GetDtoByIdAsync(int id);
    Task<ArtistDto?> GetDtoByUserIdAsync(Guid userId);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
