using Concertable.Concert.Infrastructure.Data;

namespace Concertable.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<ConcertDbContext>;

internal class UnitOfWorkBehavior(IUnitOfWork<ConcertDbContext> unitOfWork)
    : UnitOfWorkBehavior<ConcertDbContext>(unitOfWork), IUnitOfWorkBehavior;
