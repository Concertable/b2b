namespace Concertable.B2B.Authorization.Contracts;

public interface IAuthorizationContext
{
    MembershipSnapshot? Membership { get; }

    bool HasPermission(TenantPermission permission);

    ResourceCoverage CoverageFor(TenantPermission permission);
}
