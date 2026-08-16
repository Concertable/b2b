namespace Concertable.B2B.Venue.Application.Interfaces;

/// <summary>
/// The platform-admin surface over venues — cross-tenant read/write for privileged operations
/// (venue approval), served by an unfiltered writable context. Tenant-scoped access lives on
/// <see cref="IVenueRepository"/>; marketplace reads on <see cref="IVenueReadRepository"/>.
/// </summary>
internal interface IVenueAdminRepository
{
    Task<VenueEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
