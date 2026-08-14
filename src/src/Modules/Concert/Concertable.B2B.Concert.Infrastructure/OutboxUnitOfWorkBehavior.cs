using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConcertTenantDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(ConcertTenantDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<ConcertTenantDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
