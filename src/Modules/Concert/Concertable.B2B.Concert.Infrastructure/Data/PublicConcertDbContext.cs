using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

/// <summary>
/// The Concert module's public stance: the same anemic configuration as <see cref="ConcertDbContext"/>
/// with no tenant filters composed on top. Injected by the public repositories and availability
/// checks (marketplace browse, "is this slot taken?" facts) — read-only by construction.
/// </summary>
internal sealed class PublicConcertDbContext(
    DbContextOptions<PublicConcertDbContext> options,
    ConcertConfigurationProvider provider)
    : PublicDbContext(options, provider, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<OpportunityEntity> Opportunities => Set<OpportunityEntity>();
    public DbSet<ConcertRatingProjection> ConcertRatingProjections => Set<ConcertRatingProjection>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
}
