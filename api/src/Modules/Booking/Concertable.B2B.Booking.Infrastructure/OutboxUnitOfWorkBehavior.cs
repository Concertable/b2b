using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<BookingDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    BookingDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<BookingDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
