using Concertable.B2B.Booking.Infrastructure.Data;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(BookingDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
