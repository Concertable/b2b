using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConcertDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    ConcertDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<ConcertDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
