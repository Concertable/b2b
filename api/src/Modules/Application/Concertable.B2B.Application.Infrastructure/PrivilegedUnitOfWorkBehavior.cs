using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Application.Infrastructure;

internal interface IPrivilegedUnitOfWorkBehavior
    : IUnitOfWorkBehavior<ApplicationPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWorkBehavior(
    ApplicationPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<ApplicationPrivilegedDbContext>(context, unitOfWorkRunner, accessor),
        IPrivilegedUnitOfWorkBehavior;
