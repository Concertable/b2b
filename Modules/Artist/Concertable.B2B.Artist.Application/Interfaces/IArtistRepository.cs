using Concertable.B2B.Artist.Application.DTOs;
using Concertable.DataAccess.Application;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistRepository : IRepository<ArtistEntity>
{
    Task<int?> GetIdByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetByUserIdAsync(Guid id);
    Task<ArtistSummary?> GetSummaryAsync(int id);
    Task<ArtistDetails?> GetDetailsByIdAsync(int id);
    Task<ArtistDetails?> GetDetailsByUserIdAsync(Guid userId);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
