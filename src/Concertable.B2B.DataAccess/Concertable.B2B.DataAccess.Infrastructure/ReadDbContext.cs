using Concertable.B2B.Contract.Domain;
using Concertable.B2B.Conversations.Domain;
using Microsoft.EntityFrameworkCore;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal sealed class ReadDbContext : DbContextBase, IReadDbContext
{
    private readonly IEnumerable<IEntityTypeConfigurationProvider> providers;

    public ReadDbContext(
        DbContextOptions<ReadDbContext> options,
        IEnumerable<IEntityTypeConfigurationProvider> providers)
        : base(options)
    {
        this.providers = providers;
    }

    public ReadDbContext() : this(new DbContextOptionsBuilder<ReadDbContext>().Options, []) { }

    public IQueryable<UserEntity> Users => Set<UserEntity>().AsNoTracking();
    public IQueryable<ArtistEntity> Artists => Set<ArtistEntity>().AsNoTracking();
    public IQueryable<VenueEntity> Venues => Set<VenueEntity>().AsNoTracking();
    public IQueryable<VenueImageEntity> VenueImages => Set<VenueImageEntity>().AsNoTracking();
    public IQueryable<ConcertEntity> Concerts => Set<ConcertEntity>().AsNoTracking();
    public IQueryable<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>().AsNoTracking();
    public IQueryable<OpportunityEntity> Opportunities => Set<OpportunityEntity>().AsNoTracking();
    public IQueryable<ApplicationEntity> Applications => Set<ApplicationEntity>().AsNoTracking();
    public IQueryable<BookingEntity> Bookings => Set<BookingEntity>().AsNoTracking();
    public IQueryable<MessageEntity> Messages => Set<MessageEntity>().AsNoTracking();
    public IQueryable<ContractEntity> Contracts => Set<ContractEntity>().AsNoTracking();
    public IQueryable<FlatFeeContractEntity> FlatFeeContracts => Set<FlatFeeContractEntity>().AsNoTracking();
    public IQueryable<DoorSplitContractEntity> DoorSplitContracts => Set<DoorSplitContractEntity>().AsNoTracking();
    public IQueryable<VersusContractEntity> VersusContracts => Set<VersusContractEntity>().AsNoTracking();
    public IQueryable<VenueHireContractEntity> VenueHireContracts => Set<VenueHireContractEntity>().AsNoTracking();
    public IQueryable<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>().AsNoTracking();
    public IQueryable<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>().AsNoTracking();
    public IQueryable<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var provider in providers)
            provider.Configure(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("ReadDbContext is read-only.");

    public override int SaveChanges()
        => throw new NotSupportedException("ReadDbContext is read-only.");

    public override int SaveChanges(bool _)
        => throw new NotSupportedException("ReadDbContext is read-only.");
}
