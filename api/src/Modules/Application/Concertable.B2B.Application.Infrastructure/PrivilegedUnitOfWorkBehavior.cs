using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Application.Infrastructure;

internal interface IPrivilegedUnitOfWorkBehavior
    : IUnitOfWorkBehavior<ApplicationPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWorkBehavior(
    ApplicationPrivilegedDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor accessor)
    : CommandUnitOfWorkBehavior<ApplicationPrivilegedDbContext>(context, transactions, accessor),
        IPrivilegedUnitOfWorkBehavior;
