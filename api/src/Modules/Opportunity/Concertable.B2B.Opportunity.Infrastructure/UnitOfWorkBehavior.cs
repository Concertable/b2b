using Concertable.B2B.Opportunity.Infrastructure.Data;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<OpportunityDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    : Concertable.DataAccess.Infrastructure.UnitOfWorkBehavior<OpportunityDbContext>(unitOfWork), IUnitOfWorkBehavior;
