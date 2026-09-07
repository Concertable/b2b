using Concertable.B2B.Opportunity.Infrastructure.Data;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal interface IUnitOfWork : Concertable.DataAccess.Application.IUnitOfWork<OpportunityDbContext>;

internal sealed class UnitOfWork(OpportunityDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<OpportunityDbContext>(context), IUnitOfWork;
