using Concertable.B2B.Booking.Infrastructure.Data;
namespace Concertable.B2B.Booking.Infrastructure;

internal interface IUnitOfWorkBehavior
    : Concertable.DataAccess.Application.IUnitOfWorkBehavior<BookingDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    : Concertable.DataAccess.Infrastructure.UnitOfWorkBehavior<BookingDbContext>(unitOfWork), IUnitOfWorkBehavior;
