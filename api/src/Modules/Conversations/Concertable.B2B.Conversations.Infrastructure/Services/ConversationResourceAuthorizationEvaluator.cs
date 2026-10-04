using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationResourceAuthorizationEvaluator(
    ConversationsPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor) : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Conversation;

    public Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default) =>
        EvaluateAsync(request.Resource.Id, binding, actor, now, null, ct);

    public async Task<ResourceAuthorizationDecision> RequireAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("A conversation authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1 FROM conversations."Conversations"
            WHERE "Id" = {request.Resource.Id}
            FOR UPDATE
            """, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1 FROM conversations."ConversationAccessGrants"
            WHERE "ResourceId" = {request.Resource.Id}
            ORDER BY "Id"
            FOR UPDATE
            """, ct);
        return await EvaluateAsync(request.Resource.Id, binding, actor, now, null, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationSnapshot snapshot,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        if (snapshot.Request.Resource.Kind != Kind || snapshot.Binding.Resource != Kind)
            return false;

        var current = await EvaluateAsync(
            snapshot.Request.Resource.Id,
            snapshot.Binding,
            snapshot.Authority.Actor,
            now,
            snapshot,
            ct);
        return current.IsAllowed
            && current.PrincipalTenantId == snapshot.PrincipalTenantId
            && current.Grants.Length == snapshot.Grants.Length
            && current.Grants.SequenceEqual(snapshot.Grants);
    }

    private async Task<ResourceAuthorizationDecision> EvaluateAsync(
        int conversationId,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        ResourceAuthorizationSnapshot? pinned,
        CancellationToken ct)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return ResourceAuthorizationDecision.Denied;
        if (!await context.Conversations.AsNoTracking()
                .AnyAsync(conversation => conversation.Id == conversationId, ct))
            return ResourceAuthorizationDecision.Denied;

        var principal = binding.Policy == "principal_administration";
        if (!principal && binding.Policy != "conversation_grant")
            return ResourceAuthorizationDecision.Denied;

        var scopes = principal
            ? ImmutableArray.Create(nameof(ConversationAccessScope.Read))
            : binding.RequiredScopes;
        var grants = ImmutableArray.CreateBuilder<ResourceGrantSnapshot>();
        foreach (var requiredScope in scopes)
        {
            if (!Enum.TryParse<ConversationAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return ResourceAuthorizationDecision.Denied;

            var eligible = principal
                ? ConversationGrantPolicy.Principal(context.ConversationAccessGrants.AsNoTracking(), actor, now.UtcDateTime)
                : ConversationGrantPolicy.Eligible(
                    context.ConversationAccessGrants.AsNoTracking(), actor, binding.Permission, scope, now.UtcDateTime);
            eligible = eligible.Where(grant => grant.ResourceId == conversationId);
            if (pinned is not null)
            {
                var original = pinned.Grants.SingleOrDefault(grant => grant.Scope == requiredScope);
                if (original is null)
                    return ResourceAuthorizationDecision.Denied;
                eligible = eligible.Where(grant => grant.Id == original.GrantId);
            }

            var grant = await eligible.OrderBy(candidate => candidate.Id)
                .Select(candidate => new
                {
                    candidate.Id,
                    candidate.Version,
                    candidate.ValidFrom,
                    candidate.ValidUntil,
                })
                .FirstOrDefaultAsync(ct);
            if (grant is null)
                return ResourceAuthorizationDecision.Denied;

            grants.Add(new ResourceGrantSnapshot(
                requiredScope,
                grant.Id,
                grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return ResourceAuthorizationDecision.Allow(
            principal ? actor.TenantId : null,
            grants.ToImmutable());
    }
}
