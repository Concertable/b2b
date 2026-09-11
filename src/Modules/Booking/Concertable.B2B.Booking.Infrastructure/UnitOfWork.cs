using Concertable.B2B.Booking.Infrastructure.Data;

namespace Concertable.B2B.Booking.Infrastructure;

internal interface IUnitOfWork : Concertable.DataAccess.Application.IUnitOfWork<BookingDbContext>;

internal sealed class UnitOfWork(BookingDbContext context)
    : Concertable.DataAccess.Infrastructure.UnitOfWork<BookingDbContext>(context), IUnitOfWork;
