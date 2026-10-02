using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IPrivilegedUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<BookingPrivilegedDbContext>;

internal sealed class PrivilegedUnitOfWorkBehavior(
    BookingPrivilegedDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor accessor)
    : CommandUnitOfWorkBehavior<BookingPrivilegedDbContext>(context, transactions, accessor),
        IPrivilegedUnitOfWorkBehavior;
