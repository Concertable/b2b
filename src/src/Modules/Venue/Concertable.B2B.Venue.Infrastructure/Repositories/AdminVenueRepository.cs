using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class AdminVenueRepository(AdminVenueDbContext context)
    : Repository<VenueEntity, AdminVenueDbContext, int>(context), IAdminVenueRepository;
