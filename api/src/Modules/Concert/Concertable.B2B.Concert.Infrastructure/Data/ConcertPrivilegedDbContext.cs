using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="ConcertDbContext"/>.</summary>
internal sealed class ConcertPrivilegedDbContext(
    DbContextOptions<ConcertPrivilegedDbContext> options,
    ConcertConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceSequenceEntity> InvoiceSequences => Set<InvoiceSequenceEntity>();
    public DbSet<SelfBillingAgreementEntity> SelfBillingAgreements => Set<SelfBillingAgreementEntity>();
    public DbSet<ConcertImageEntity> ConcertImages => Set<ConcertImageEntity>();
    public DbSet<ArtistReadModel> ArtistReadModels => Set<ArtistReadModel>();
    public DbSet<VenueReadModel> VenueReadModels => Set<VenueReadModel>();
    public DbSet<ConcertAccessGrant> ConcertAccessGrants => Set<ConcertAccessGrant>();
    public DbSet<InvoiceAccessGrant> InvoiceAccessGrants => Set<InvoiceAccessGrant>();
    public DbSet<ConcertCommandReceipt> ConcertCommandReceipts => Set<ConcertCommandReceipt>();
}
