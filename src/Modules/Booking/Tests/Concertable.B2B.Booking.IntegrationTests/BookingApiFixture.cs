using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.IntegrationTests;

public sealed class BookingApiFixture : ApiFixture
{
    private IBookingReadDbContext context = null!;

    internal IQueryable<BookingEntity> Bookings => context.Bookings;
    internal IQueryable<ContractEntity> Contracts => context.Contracts;

    protected override void OnReset(IServiceScope scope)
    {
        context = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
    }
}
