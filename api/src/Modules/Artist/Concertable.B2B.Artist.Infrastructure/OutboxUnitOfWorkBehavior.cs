using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Artist.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ArtistDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    ArtistDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor commandAccessor,
    IDbContextAccessor outboxAccessor)
    : CommandOutboxUnitOfWorkBehavior<ArtistDbContext>(
        context, transactions, commandAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
