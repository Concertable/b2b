using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistRepository : ITenantScopedRepository<ArtistEntity>
{
    Task<int?> GetIdByUserIdAsync(Guid id);
    Task<ArtistEntity?> GetByUserIdAsync(Guid id);
    Task<ArtistDetails?> GetDetailsByUserIdAsync(Guid userId);
}
