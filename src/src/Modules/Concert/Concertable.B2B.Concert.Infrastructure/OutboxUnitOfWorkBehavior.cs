using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<TenantConcertDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(TenantConcertDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<TenantConcertDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
