using Concertable.DataAccess.Application;

namespace Concertable.B2B.Venue.Application.Interfaces;

/// <summary>
/// The platform-admin surface over venues — cross-tenant read/write for privileged operations
/// (venue approval), served by an unfiltered writable context. Tenant-scoped access lives on
/// <see cref="IVenueRepository"/>; marketplace reads on <see cref="IVenueReadRepository"/>.
/// </summary>
internal interface IVenuePrivilegedRepository : IRepository<VenueEntity>
{
    Task<IPagination<VenueEntity>> GetPendingApprovalAsync(IPageParams pageParams);
}
