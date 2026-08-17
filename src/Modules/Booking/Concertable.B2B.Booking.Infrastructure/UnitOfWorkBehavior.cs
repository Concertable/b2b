using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IUnitOfWorkBehavior : IUnitOfWorkBehavior<BookingDbContext>;

internal sealed class UnitOfWorkBehavior(IUnitOfWork<BookingDbContext> unitOfWork)
    : UnitOfWorkBehavior<BookingDbContext>(unitOfWork), IUnitOfWorkBehavior;
