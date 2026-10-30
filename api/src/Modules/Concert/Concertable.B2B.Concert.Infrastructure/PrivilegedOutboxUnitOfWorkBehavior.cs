using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IPrivilegedOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConcertPrivilegedDbContext>;

internal sealed class PrivilegedOutboxUnitOfWorkBehavior(
    ConcertPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<ConcertPrivilegedDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IPrivilegedOutboxUnitOfWorkBehavior;
