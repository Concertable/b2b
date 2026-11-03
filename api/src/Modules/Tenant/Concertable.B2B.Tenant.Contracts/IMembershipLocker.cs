using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Contracts;

public interface IMembershipLocker
{
    Task<Option<LockedMemberships>> LockAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default);
}
