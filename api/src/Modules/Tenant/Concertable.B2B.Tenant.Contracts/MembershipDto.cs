namespace Concertable.B2B.Tenant.Contracts;

public sealed record MembershipDto(
    Guid TenantId,
    string LegalName,
    TenantRole Role,
    IReadOnlyList<TenantBusinessProfileKind> BusinessProfiles,
    IReadOnlyList<string> Permissions);
