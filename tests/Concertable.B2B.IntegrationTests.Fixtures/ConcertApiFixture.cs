using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class ConcertApiFixture : ApiFixture
{
    private ApplicationDbContext applicationReads = null!;
    private BookingDbContext bookingReads = null!;
    private ConcertReadDbContext concertReads = null!;

    public DbContext ApplicationReads => applicationReads;
    public DbContext BookingReads => bookingReads;

    /// <summary>
    /// The Concert module's unfiltered, read-only read stance — sees every tenant's rows, so
    /// cross-tenant assertions can read what the tenant-filtered context would hide.
    /// </summary>
    public ReadDbContext ConcertReads => concertReads;

    protected override void OnReset(IServiceScope scope)
    {
        applicationReads = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        bookingReads = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        concertReads = scope.ServiceProvider.GetRequiredService<ConcertReadDbContext>();
    }
}
