using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IPrivilegedOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ConcertPrivilegedDbContext>;

internal sealed class PrivilegedOutboxUnitOfWorkBehavior(
    ConcertPrivilegedDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<ConcertPrivilegedDbContext>(context, accessor), IPrivilegedOutboxUnitOfWorkBehavior;
