using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Venue.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<TenantVenueDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(TenantVenueDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<TenantVenueDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
