using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Contracts;

public sealed record TenantCommandFacts(
    MembershipSnapshot Actor,
    bool TargetTenantExists,
    MembershipSnapshot? TargetMembership);

public interface ITenantCommandFacts
{
    Task<TenantCommandFacts?> ResolveAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default);
}
