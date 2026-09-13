using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Venue.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<VenueDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(VenueDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<VenueDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
