using Concertable.B2B.Concert.Infrastructure.Data;
namespace Concertable.B2B.Concert.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<ConcertDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    : Concertable.DataAccess.Infrastructure.UnitOfWorkBehavior<ConcertDbContext>(unitOfWork), IUnitOfWorkBehavior;
