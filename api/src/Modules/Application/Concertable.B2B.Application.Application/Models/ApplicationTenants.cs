namespace Concertable.B2B.Application.Application.Models;

/// <summary>The two tenants an application is economically between, as the current presets still need them.</summary>
internal readonly record struct ApplicationTenants(Guid VenueTenantId, Guid ArtistTenantId);
