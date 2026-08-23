using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenuePrivilegedRepository(VenuePrivilegedDbContext context)
    : Repository<VenueEntity, int>(context), IVenuePrivilegedRepository
{
    public Task<IPagination<VenueEntity>> GetPendingApprovalAsync(IPageParams pageParams) =>
        Context.Query<VenueEntity>()
            .Where(v => !v.Approved)
            .OrderBy(v => v.Id)
            .ToPaginationAsync(pageParams);
}
