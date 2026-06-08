using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<ConcertDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<ConcertDbContext> unitOfWork)
    : UnitOfWorkBehavior<ConcertDbContext>(unitOfWork), IUnitOfWorkBehavior;
