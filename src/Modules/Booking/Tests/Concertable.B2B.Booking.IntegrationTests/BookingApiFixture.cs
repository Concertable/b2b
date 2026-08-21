using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.IntegrationTests;

public sealed class BookingApiFixture : ApiFixture
{
    private IBookingReadDbContext context = null!;
    private BookingDbContext writeContext = null!;

    internal IQueryable<BookingEntity> Bookings => context.Bookings;
    internal IQueryable<ContractEntity> Contracts => context.Contracts;
    internal IQueryable<InboxMessageEntity> InboxMessages => writeContext.Set<InboxMessageEntity>().AsNoTracking();

    protected override void OnReset(IServiceScope scope)
    {
        context = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
        writeContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    }
}
