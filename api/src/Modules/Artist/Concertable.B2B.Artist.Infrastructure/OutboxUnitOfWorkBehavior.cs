using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Artist.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ArtistDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(
    ArtistDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IDbContextAccessor outboxAccessor)
    : Concertable.B2B.DataAccess.Infrastructure.OutboxUnitOfWorkBehavior<ArtistDbContext>(
        context, unitOfWorkRunner, unitOfWorkAccessor, outboxAccessor),
        IOutboxUnitOfWorkBehavior;
