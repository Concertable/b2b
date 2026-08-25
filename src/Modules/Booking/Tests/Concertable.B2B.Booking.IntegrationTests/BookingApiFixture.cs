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

    internal Task FailBookingUpdatesAsync() =>
        dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER [booking].[TR_Bookings_FailUpdate_ForTest]
            ON [booking].[Bookings]
            AFTER UPDATE
            AS
            BEGIN
                THROW 51000, 'Forced booking update failure.', 1;
            END
            """);

    internal Task RestoreBookingUpdatesAsync() =>
        dbContext.Database.ExecuteSqlRawAsync(
            "DROP TRIGGER IF EXISTS [booking].[TR_Bookings_FailUpdate_ForTest]");

    internal Task<int> GetConcertCountAsync(int bookingId) =>
        dbContext.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value]
                FROM [concert].[Concerts]
                WHERE [BookingId] = {bookingId}
                """)
            .SingleAsync();

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    }
}
