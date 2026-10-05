using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IPrivilegedUnitOfWorkBehavior
    : IUnitOfWorkBehavior<ConcertPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWorkBehavior(
    ConcertPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<ConcertPrivilegedDbContext>(context, unitOfWorkRunner, accessor),
        IPrivilegedUnitOfWorkBehavior;
