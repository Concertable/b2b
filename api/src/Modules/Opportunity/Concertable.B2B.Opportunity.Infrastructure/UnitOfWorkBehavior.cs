using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<OpportunityDbContext>;

internal sealed class UnitOfWorkBehavior(
    OpportunityDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<OpportunityDbContext>(context, unitOfWorkRunner, accessor), IUnitOfWorkBehavior;

internal interface IPrivilegedUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<OpportunityPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWorkBehavior(
    OpportunityPrivilegedDbContext context,
    UnitOfWorkRunner unitOfWorkRunner,
    UnitOfWorkAccessor accessor)
    : Concertable.B2B.DataAccess.Infrastructure.UnitOfWorkBehavior<OpportunityPrivilegedDbContext>(context, unitOfWorkRunner, accessor),
        IPrivilegedUnitOfWorkBehavior;
