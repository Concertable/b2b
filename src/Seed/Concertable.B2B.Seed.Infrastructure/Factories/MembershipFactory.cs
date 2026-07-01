using System.Security.Cryptography;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.Seed.Identity.Extensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class MembershipFactory
{
    /// <summary>
    /// The founding Owner membership for a seed operator. Deterministic id (distinct from the tenant id, which
    /// is a hash of the user id alone) keeps the seeder re-runnable; the provisioning handler dedups over it by
    /// <c>(TenantId, UserId)</c>, so seed-then-register produces exactly one membership whatever the ordering.
    /// </summary>
    public static TenantMembershipEntity FoundingOwner(Guid tenantId, Guid userId, DateTime createdAt) =>
        TenantMembershipEntity.Create(tenantId, userId, TenantRole.Owner, invitedBy: null, createdAt)
            .With(nameof(TenantMembershipEntity.Id), DeterministicId(tenantId, userId));

    private static Guid DeterministicId(Guid tenantId, Guid userId) =>
        new(MD5.HashData([.. tenantId.ToByteArray(), .. userId.ToByteArray()]));
}
