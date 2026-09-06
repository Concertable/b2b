using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Domain;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.IntegrationTests;

public sealed class BookingApiFixture : ApiFixture
{
    private IBookingReadDbContext readDbContext = null!;
    private BookingDbContext dbContext = null!;

    internal ConcurrencyConflictInterceptor Conflicts { get; } = new();

    internal IQueryable<BookingEntity> Bookings => readDbContext.Bookings;
    internal IQueryable<ContractEntity> Contracts => readDbContext.Contracts;
    internal IQueryable<InboxMessageEntity> InboxMessages => dbContext.Set<InboxMessageEntity>().AsNoTracking();

    /// <summary>
    /// Commits <paramref name="competingChange"/> between the next booking transition's read and its
    /// update, so that transition loses the race and has to rerun against the winner's state.
    /// </summary>
    internal void ArmBookingConflict(Func<Task> competingChange) =>
        Conflicts.ArmOnce<BookingEntity>(competingChange);

    // A CHECK constraint rather than a trigger: EF reads the row version back with an OUTPUT clause, and SQL
    // Server rejects OUTPUT against a table that has an enabled trigger. It must name a column every booking
    // update writes -- SQL Server skips constraints whose columns the UPDATE leaves alone -- and NOCHECK
    // keeps the rows already seeded valid.
    internal Task FailBookingUpdatesAsync() =>
        dbContext.Database.ExecuteSqlRawAsync("""
            ALTER TABLE [booking].[Bookings] WITH NOCHECK
            ADD CONSTRAINT [CK_Bookings_FailUpdate_ForTest] CHECK ([State] IS NULL)
            """);

    internal Task RestoreBookingUpdatesAsync() =>
        dbContext.Database.ExecuteSqlRawAsync("""
            IF EXISTS (
                SELECT 1 FROM sys.check_constraints
                WHERE [name] = 'CK_Bookings_FailUpdate_ForTest')
                ALTER TABLE [booking].[Bookings] DROP CONSTRAINT [CK_Bookings_FailUpdate_ForTest]
            """);

    internal Task<int> GetConcertCountAsync(int bookingId) =>
        dbContext.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value]
                FROM [concert].[Concerts]
                WHERE [BookingId] = {bookingId}
                """)
            .SingleAsync();

    /// <summary>
    /// Runs the event's pre-commit handlers the way <c>DomainEventDispatcher</c> does: handlers register
    /// against <see cref="IDomainEventHandler{TEvent}"/> and the phase is chosen by the marker, so resolving
    /// the marker interface directly would resolve nothing.
    /// </summary>
    internal Task DispatchPreCommitDomainEventAsync<TEvent>(TEvent @event)
        where TEvent : IDomainEvent =>
        Services.GetRequiredService<IScoped<IEnumerable<IDomainEventHandler<TEvent>>>>()
            .RunAsync(async handlers =>
            {
                foreach (var handler in handlers.OfType<IPreCommitDomainEventHandler<TEvent>>())
                    await handler.HandleAsync(@event);
            });

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(Conflicts);
        services.ConfigureDbContext<BookingDbContext>(
            (_, options) => options.AddInterceptors(Conflicts));
    }

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IBookingReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    }
}
