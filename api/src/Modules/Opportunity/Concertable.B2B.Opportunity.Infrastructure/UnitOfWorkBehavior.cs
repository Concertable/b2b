using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<OpportunityDbContext>;

internal sealed class UnitOfWorkBehavior(
    OpportunityDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor accessor)
    : CommandUnitOfWorkBehavior<OpportunityDbContext>(context, transactions, accessor), IUnitOfWorkBehavior;
