namespace Concertable.B2B.Tenant.Contracts;

public sealed record MembershipDto(
    Guid MembershipId,
    Guid TenantId,
    string LegalName,
    TenantRole Role,
    long PermissionVersion,
    IReadOnlyList<TenantBusinessActivityKind> BusinessActivities,
    IReadOnlyList<string> Permissions);
