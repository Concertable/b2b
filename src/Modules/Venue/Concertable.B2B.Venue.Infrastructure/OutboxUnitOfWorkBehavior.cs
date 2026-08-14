using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Venue.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<VenueTenantDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(VenueTenantDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<VenueTenantDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
