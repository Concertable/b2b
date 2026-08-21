using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.IntegrationTests;

public sealed class BookingApiFixture : ApiFixture
{
    private IBookingReadDbContext readDbContext = null!;
    private BookingDbContext dbContext = null!;

    internal IQueryable<BookingEntity> Bookings => readDbContext.Bookings;
    internal IQueryable<ContractEntity> Contracts => readDbContext.Contracts;
    internal IQueryable<InboxMessageEntity> InboxMessages => dbContext.Set<InboxMessageEntity>().AsNoTracking();

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    }
}
