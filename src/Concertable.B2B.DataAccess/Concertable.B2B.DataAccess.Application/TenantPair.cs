namespace Concertable.B2B.DataAccess.Application;

/// <summary>Both party ids off a two-party row.</summary>
public sealed record TenantPair(Guid VenueTenantId, Guid ArtistTenantId);
