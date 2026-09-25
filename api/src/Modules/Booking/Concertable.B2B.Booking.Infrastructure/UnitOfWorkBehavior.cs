using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
namespace Concertable.B2B.Booking.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<BookingDbContext>;

internal sealed class UnitOfWorkBehavior(
    BookingDbContext context,
    CommandTransactionFactory transactions,
    CommandTransactionAccessor accessor)
    : CommandUnitOfWorkBehavior<BookingDbContext>(context, transactions, accessor), IUnitOfWorkBehavior;
