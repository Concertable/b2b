using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Payment.Domain;
using Concertable.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class ConcertApiFixture : ApiFixture
{
    private PublicConcertDbContext concertReads = null!;
    private PaymentDbContext paymentDbContext = null!;

    /// <summary>
    /// The Concert module's unfiltered, read-only read stance — sees every tenant's rows, so
    /// cross-tenant assertions can read what the tenant-filtered context would hide.
    /// </summary>
    public PublicDbContext ConcertReads => concertReads;

    public IQueryable<EscrowEntity> Escrows => paymentDbContext.Escrows.AsNoTracking();

    protected override void OnReset(IServiceScope scope)
    {
        concertReads = scope.ServiceProvider.GetRequiredService<PublicConcertDbContext>();
        paymentDbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    }
}
