using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IPrivilegedOutboxUnitOfWorkBehavior
    : IOutboxUnitOfWorkBehavior<BookingPrivilegedDbContext>;

internal sealed class PrivilegedOutboxUnitOfWorkBehavior(
    BookingPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<BookingPrivilegedDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IPrivilegedOutboxUnitOfWorkBehavior;
