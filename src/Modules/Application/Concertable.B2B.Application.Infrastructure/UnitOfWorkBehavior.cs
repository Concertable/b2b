using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Application.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<ApplicationDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<ApplicationDbContext> unitOfWork)
    : UnitOfWorkBehavior<ApplicationDbContext>(unitOfWork), IUnitOfWorkBehavior;
