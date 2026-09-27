using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Contracts;

public sealed record TenantCommandFacts(
    MembershipSnapshot Actor,
    bool TargetTenantExists,
    MembershipSnapshot? TargetMembership);

public sealed record TenantAudienceFacts(
    MembershipSnapshot Actor,
    IReadOnlySet<Guid> ExistingTenantIds);

public interface ITenantCommandFacts
{
    Task<TenantCommandFacts?> ResolveAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default);

    Task<TenantAudienceFacts?> ResolveAudienceAsync(
        MembershipSnapshot expectedActor,
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default);
}
