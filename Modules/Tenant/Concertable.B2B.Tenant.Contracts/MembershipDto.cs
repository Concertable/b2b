namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant the caller belongs to, with their role in it — the unit the tenant switcher lists from
/// <c>GET /api/auth/me</c>. <see cref="LegalName"/> labels the switcher entry; <see cref="Type"/> drives
/// the persona-specific UI.
/// </summary>
public sealed record MembershipDto(Guid TenantId, string LegalName, TenantType Type, TenantRole Role);
