using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.IntegrationTests;

public sealed class BookingApiFixture : ApiFixture
{
    private IBookingReadDbContext readDbContext = null!;
    private BookingDbContext dbContext = null!;

    internal IQueryable<BookingEntity> Bookings => readDbContext.Bookings;
    internal IQueryable<ContractEntity> Contracts => readDbContext.Contracts;
    internal IQueryable<InboxMessageEntity> InboxMessages => dbContext.Set<InboxMessageEntity>().AsNoTracking();

    internal async Task<IDbContextTransaction> HoldBookingForUpdateAsync(int bookingId)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            _ = await dbContext.Database.SqlQuery<int>($"""
                    SELECT [Id] AS [Value]
                    FROM [booking].[Bookings] WITH (UPDLOCK, ROWLOCK)
                    WHERE [Id] = {bookingId}
                    """)
                .SingleAsync();
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    internal async Task WaitForBookingLockWaitersAsync(int expectedCount)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var count = await dbContext.Database.SqlQuery<int>($"""
                    SELECT COUNT(*) AS [Value]
                    FROM sys.dm_exec_requests AS request
                    CROSS APPLY sys.dm_exec_sql_text(request.sql_handle) AS batch
                    WHERE request.wait_type LIKE N'LCK_M_%'
                      AND CHARINDEX(
                          N'FROM [booking].[Bookings] WITH (UPDLOCK, ROWLOCK)',
                          batch.text) > 0
                    """)
                .SingleAsync();
            if (count >= expectedCount)
                return;

            await Task.Delay(25);
        }

        throw new InvalidOperationException(
            $"Expected {expectedCount} booking transition lock waiter(s).");
    }

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

    internal Task DispatchPreCommitDomainEventAsync<TEvent>(TEvent @event)
        where TEvent : IDomainEvent =>
        Services.GetRequiredService<IScoped<IEnumerable<IPreCommitDomainEventHandler<TEvent>>>>()
            .RunAsync(async handlers =>
            {
                foreach (var handler in handlers)
                    await handler.HandleAsync(@event);
            });

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    }
}
