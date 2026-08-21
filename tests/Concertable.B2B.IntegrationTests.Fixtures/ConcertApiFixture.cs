using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class ConcertApiFixture : ApiFixture
{
    private ApplicationDbContext applicationDb = null!;
    private BookingDbContext bookingDb = null!;
    private ConcertReadDbContext concertReads = null!;

    public DbContext ApplicationDb => applicationDb;
    public DbContext BookingDb => bookingDb;

    /// <summary>
    /// The Concert module's unfiltered, read-only read stance — sees every tenant's rows, so
    /// cross-tenant assertions can read what the tenant-filtered context would hide.
    /// </summary>
    public ReadDbContext ConcertReads => concertReads;

    protected override void OnReset(IServiceScope scope)
    {
        applicationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        bookingDb = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        concertReads = scope.ServiceProvider.GetRequiredService<ConcertReadDbContext>();
    }
}
