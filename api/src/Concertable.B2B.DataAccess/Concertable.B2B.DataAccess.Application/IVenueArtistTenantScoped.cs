using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// A row shared by exactly two tenants — the venue side and the artist side — such as an
/// application, booking or concert. Carries both tenant ids denormalized at write time (the
/// frozen-at-accept snapshot settlement reads), and drives the looped two-tenant
/// <c>"Tenant"</c> query filter (<c>venue == me || artist == me</c>). The single-owner
/// counterpart is <see cref="ITenantScoped"/>.
/// </summary>
public interface IVenueArtistTenantScoped
{
    /// <summary>
    /// The venue-side tenant captured when the entity is created.
    /// </summary>
    Guid VenueTenantId { get; }

    /// <summary>
    /// The artist-side tenant captured when the entity is created.
    /// </summary>
    Guid ArtistTenantId { get; }
}
