using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Deal.Infrastructure.Data;

internal sealed class DealPrivilegedDbContext(
    DbContextOptions<DealPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    DealConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<DealEntity> Deals => Set<DealEntity>();
    public DbSet<FlatFeeDealEntity> FlatFeeDeals => Set<FlatFeeDealEntity>();
    public DbSet<DoorSplitDealEntity> DoorSplitDeals => Set<DoorSplitDealEntity>();
    public DbSet<VersusDealEntity> VersusDeals => Set<VersusDealEntity>();
    public DbSet<VenueHireDealEntity> VenueHireDeals => Set<VenueHireDealEntity>();
}
