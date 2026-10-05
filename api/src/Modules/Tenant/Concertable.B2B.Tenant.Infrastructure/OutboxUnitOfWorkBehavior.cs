using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Tenant.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<TenantDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    TenantDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<TenantDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
