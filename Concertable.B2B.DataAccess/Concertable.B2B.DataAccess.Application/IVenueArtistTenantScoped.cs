using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// A row shared by exactly two tenants — the venue side and the artist side — such as an
/// application, booking or concert. Carries both party ids denormalized at write time (the
/// frozen-at-accept snapshot settlement reads), and drives the looped two-party
/// <c>"Tenant"</c> query filter (<c>venue == me || artist == me</c>). The single-owner
/// counterpart is <see cref="ITenantScoped"/>.
/// </summary>
public interface IVenueArtistTenantScoped
{
    /// <summary>
    /// The venue-side tenant. Settable so the owning workflow can stamp it at apply/accept
    /// (mirroring <see cref="ITenantScoped"/>); domain code never sets it directly.
    /// </summary>
    Guid VenueTenantId { get; set; }

    /// <summary>
    /// The artist-side tenant. Settable so the owning workflow can stamp it at apply/accept
    /// (mirroring <see cref="ITenantScoped"/>); domain code never sets it directly.
    /// </summary>
    Guid ArtistTenantId { get; set; }
}
