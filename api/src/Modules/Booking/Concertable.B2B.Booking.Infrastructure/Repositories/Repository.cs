using Concertable.B2B.Booking.Infrastructure.Data;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(BookingDbContext context)
    : Repository<TEntity, int>(context)
    where TEntity : class, IIdEntity;
