using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="DealDbContext"/>.</summary>
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
