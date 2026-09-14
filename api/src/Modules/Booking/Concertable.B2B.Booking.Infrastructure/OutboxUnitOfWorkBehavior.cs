using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<BookingDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    BookingDbContext context,
    IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<BookingDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
