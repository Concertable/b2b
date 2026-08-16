using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConcertDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(ConcertDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<ConcertDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
