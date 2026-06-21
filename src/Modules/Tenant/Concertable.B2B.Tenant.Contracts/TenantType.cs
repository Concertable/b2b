namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant's persona, fixed at provisioning from the registration client-id (venue-* → Venue, artist-* →
/// Artist). Drives UI and the persona constraints on permissions — the replacement for the old
/// venue/artist manager split. Set once; a tenant never changes persona.
/// </summary>
public enum TenantType
{
    Venue = 1,
    Artist = 2,
}
