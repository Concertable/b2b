using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueAdminRepository(VenueAdminDbContext context)
    : Repository<VenueEntity, int>(context), IVenueAdminRepository;
