using Concertable.B2B.Application.Infrastructure.Data;
namespace Concertable.B2B.Application.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<ApplicationDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    : Concertable.DataAccess.Infrastructure.UnitOfWorkBehavior<ApplicationDbContext>(unitOfWork), IUnitOfWorkBehavior;
