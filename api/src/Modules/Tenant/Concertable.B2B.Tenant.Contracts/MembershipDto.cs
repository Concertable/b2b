namespace Concertable.B2B.Tenant.Contracts;

public sealed record MembershipDto(
    Guid MembershipId,
    Guid TenantId,
    string LegalName,
    IReadOnlyList<RoleSummary> Roles,
    long PermissionVersion,
    long RolePolicyVersion,
    IReadOnlyList<TenantBusinessActivityKind> BusinessActivities,
    IReadOnlyList<string> Permissions);
