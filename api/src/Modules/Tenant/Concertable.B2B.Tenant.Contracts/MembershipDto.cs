namespace Concertable.B2B.Tenant.Contracts;

public sealed record MembershipDto(Guid TenantId, string LegalName, TenantType Type, TenantRole Role);
