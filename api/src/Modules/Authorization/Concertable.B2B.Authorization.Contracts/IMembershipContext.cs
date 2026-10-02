namespace Concertable.B2B.Authorization.Contracts;

public interface IMembershipContext
{
    MembershipSnapshot? Membership { get; }

    bool HasPermission(TenantPermission permission);

    ResourceAudience AudienceFor(TenantPermission permission);
}
