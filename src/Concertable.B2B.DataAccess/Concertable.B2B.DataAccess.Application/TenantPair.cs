namespace Concertable.B2B.DataAccess.Application;

/// <summary>Both party ids off a two-party row.</summary>
public readonly record struct TenantPair(Guid VenueTenantId, Guid ArtistTenantId);
