using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<ConcertDbContext>;

internal sealed class UnitOfWorkBehavior(
    ConcertDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<ConcertDbContext>(context, unitOfWorkRunner, accessor), IUnitOfWorkBehavior;
