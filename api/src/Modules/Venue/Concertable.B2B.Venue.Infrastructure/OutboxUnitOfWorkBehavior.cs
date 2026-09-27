using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Venue.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<VenueDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    VenueDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor commandAccessor,
    IDbContextAccessor outboxAccessor)
    : CommandOutboxUnitOfWorkBehavior<VenueDbContext>(
        context, transactions, commandAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
