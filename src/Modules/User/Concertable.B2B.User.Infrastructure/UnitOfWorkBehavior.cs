using Concertable.B2B.User.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.User.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<UserDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<UserDbContext> unitOfWork)
    : UnitOfWorkBehavior<UserDbContext>(unitOfWork), IUnitOfWorkBehavior;
