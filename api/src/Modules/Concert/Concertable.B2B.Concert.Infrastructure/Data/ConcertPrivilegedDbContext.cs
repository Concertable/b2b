using Concertable.B2B.Artist.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal sealed class ConcertPrivilegedDbContext(
    DbContextOptions<ConcertPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ConcertConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
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
