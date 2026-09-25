using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
namespace Concertable.B2B.Application.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<ApplicationDbContext>;

internal sealed class UnitOfWorkBehavior(
    ApplicationDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor accessor)
    : CommandUnitOfWorkBehavior<ApplicationDbContext>(context, transactions, accessor), IUnitOfWorkBehavior;
