using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Venue.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<VenueDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    VenueDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<VenueDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
