using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Conversations.Domain.Entities;

/// <summary>
/// One conversation, with an identity of its own. It replaces the implicit venue/artist tuple that used to
/// be the thread: a conversation between three businesses, or between two that are neither a venue nor an
/// artist, has nowhere to live in a tuple of two fixed roles. Who can see it is its grants, so a thread can
/// be disclosed to a third business without rewriting every message in it.
/// </summary>
public sealed class ThreadEntity : IIdEntity
{
    private ThreadEntity() { }

    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<ThreadAccessGrant> accessGrants = [];
    public IReadOnlyList<ThreadAccessGrant> AccessGrants => accessGrants;

    public static ThreadEntity Create(IReadOnlyCollection<Guid> participantTenantIds, DateTime at)
    {
        ArgumentNullException.ThrowIfNull(participantTenantIds);
        if (participantTenantIds.Count == 0)
            throw new ArgumentException("A thread requires at least one participant.", nameof(participantTenantIds));

        var thread = new ThreadEntity { CreatedAt = at };
        foreach (var tenantId in participantTenantIds.Distinct())
            thread.AdmitPrincipal(tenantId, at);

        return thread;
    }

    private void AdmitPrincipal(Guid tenantId, DateTime at)
    {
        foreach (var scope in new[] { ThreadAccessScope.Read, ThreadAccessScope.SendMessages })
        {
            accessGrants.Add(ThreadAccessGrant.Issue(
                Id,
                tenantId,
                membershipId: null,
                scope,
                issuedByTenantId: tenantId,
                issuedByUserId: null,
                ResourceGrantKind.Principal,
                at));
        }
    }

    public bool Admits(Guid tenantId, ThreadAccessScope scope, DateTime at) =>
        accessGrants.Exists(grant =>
            grant.TenantId == tenantId && grant.Scope == scope && grant.IsLiveAt(at));
}
