namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>The display name and business email of the venue or artist a tenant owns, as the Tenant module
/// sees it — the canonical shape <see cref="Interfaces.ITenantContactResolver"/> resolves each
/// <see cref="TenantType"/>'s module-owned contact into.</summary>
internal readonly record struct TenantContact(string Name, string Email);
