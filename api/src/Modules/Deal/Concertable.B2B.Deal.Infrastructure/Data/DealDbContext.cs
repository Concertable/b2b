using Concertable.B2B.Deal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealDbContext(
    DbContextOptions<DealDbContext> options,
    DealConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<DealEntity> Deals => Set<DealEntity>();
    public DbSet<FlatFeeDealEntity> FlatFeeDeals => Set<FlatFeeDealEntity>();
    public DbSet<DoorSplitDealEntity> DoorSplitDeals => Set<DoorSplitDealEntity>();
    public DbSet<VersusDealEntity> VersusDeals => Set<VersusDealEntity>();
    public DbSet<VenueHireDealEntity> VenueHireDeals => Set<VenueHireDealEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
