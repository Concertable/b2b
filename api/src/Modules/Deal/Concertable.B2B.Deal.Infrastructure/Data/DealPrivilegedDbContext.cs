using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data;

/// <summary>
/// The Deal module's stance for work no human is acting in: the same mapping as <see cref="DealDbContext"/>
/// without the tenant write fence, because a deal belongs to a tenant and this stance writes for many. Only
/// seed factories and this module's own system work may inject it.
/// </summary>
internal sealed class DealPrivilegedDbContext(
    DbContextOptions<DealPrivilegedDbContext> options,
    DealConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<DealEntity> Deals => Set<DealEntity>();
    public DbSet<FlatFeeDealEntity> FlatFeeDeals => Set<FlatFeeDealEntity>();
    public DbSet<DoorSplitDealEntity> DoorSplitDeals => Set<DoorSplitDealEntity>();
    public DbSet<VersusDealEntity> VersusDeals => Set<VersusDealEntity>();
    public DbSet<VenueHireDealEntity> VenueHireDeals => Set<VenueHireDealEntity>();
}
