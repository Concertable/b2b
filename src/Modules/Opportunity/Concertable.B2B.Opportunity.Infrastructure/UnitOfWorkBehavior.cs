using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<OpportunityDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<OpportunityDbContext> unitOfWork)
    : UnitOfWorkBehavior<OpportunityDbContext>(unitOfWork), IUnitOfWorkBehavior;
