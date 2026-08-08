using System.Security.Cryptography;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
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
        Member(tenantId, userId, TenantRole.Owner, invitedBy: null, createdAt);

    /// <summary>A seeded membership in an existing tenant with an explicit role — the invited-colleague shape. Shares
    /// <see cref="FoundingOwner"/>'s deterministic id so seed-then-register dedups over <c>(TenantId, UserId)</c>.</summary>
    public static TenantMembershipEntity Member(Guid tenantId, Guid userId, TenantRole role, Guid? invitedBy, DateTime createdAt) =>
        TenantMembershipEntity.Create(tenantId, userId, role, invitedBy, createdAt)
            .With(nameof(TenantMembershipEntity.Id), DeterministicId(tenantId, userId));

    private static Guid DeterministicId(Guid tenantId, Guid userId) =>
        new(MD5.HashData([.. tenantId.ToByteArray(), .. userId.ToByteArray()]));
}
