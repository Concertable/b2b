using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class ConcertApiFixture : ApiFixture
{
    private ConcertDbContext concertReads = null!;

    /// <summary>
    /// The Concert module's unfiltered, read-only read stance — sees every tenant's rows, so
    /// cross-tenant assertions can read what the tenant-filtered context would hide.
    /// </summary>
    public ReadDbContext ConcertReads => concertReads;

    protected override void OnReset(IServiceScope scope)
    {
        concertReads = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
    }
}
