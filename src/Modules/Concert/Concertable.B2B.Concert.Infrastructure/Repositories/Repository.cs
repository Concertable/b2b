using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ConcertDbContext context)
    : BaseRepository<TEntity, ConcertDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(ConcertDbContext context)
    : ReadRepository<TEntity, ConcertDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(ConcertDbContext context)
    : Repository<TEntity, ConcertDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(ConcertDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, ConcertDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(ConcertDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, ConcertDbContext, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;

internal abstract class OpportunityRepository<TContext> : TenantScopedRepository<OpportunityEntity, TContext, int>
    where TContext : DbContextBase
{
    private readonly TimeProvider timeProvider;

    protected OpportunityRepository(TContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await ActiveForVenue(venueId).ToListAsync();

    protected IQueryable<OpportunityEntity> ActiveForVenue(int venueId) =>
        context.Set<OpportunityEntity>()
            .Where(o => o.VenueId == venueId)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(o => o.Period.Start);
}
