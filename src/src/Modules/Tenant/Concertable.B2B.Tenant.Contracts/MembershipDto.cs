namespace Concertable.B2B.Tenant.Contracts;

/// <summary>A tenant membership displayed by the tenant switcher.</summary>
public sealed record MembershipDto(Guid TenantId, string LegalName, TenantType Type, TenantRole Role);
