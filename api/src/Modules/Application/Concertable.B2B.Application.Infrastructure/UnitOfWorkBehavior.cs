using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
namespace Concertable.B2B.Application.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<ApplicationDbContext>;

internal sealed class UnitOfWorkBehavior(
    ApplicationDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<ApplicationDbContext>(context, unitOfWorkRunner, accessor), IUnitOfWorkBehavior;
