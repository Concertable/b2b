using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistRepository : ITenantScopedRepository<ArtistEntity>
{
    Task<int?> GetIdForCurrentTenantAsync();
    Task<ArtistDetails?> GetDetailsForCurrentTenantAsync();
}
