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

    public Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default) =>
        ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, ct);

    public async Task<ResourceAuthorizationEvidence?> RequireAsync(
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
        return await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        if (proof.Request.Resource.Kind != Kind || proof.Binding.Resource != Kind)
            return false;

        var current = await ReadEvidenceAsync(
            proof.Request.Resource.Id,
            proof.Binding,
            proof.Authority.Actor,
            now,
            proof.Evidence,
            ct);
        return current is not null
            && current.PrincipalTenantId == proof.Evidence.PrincipalTenantId
            && current.Grants.Length == proof.Evidence.Grants.Length
            && current.Grants.SequenceEqual(proof.Evidence.Grants);
    }

    private async Task<ResourceAuthorizationEvidence?> ReadEvidenceAsync(
        int conversationId,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        ResourceAuthorizationEvidence? pinned,
        CancellationToken ct)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return null;
        if (!await context.Conversations.AsNoTracking()
                .AnyAsync(conversation => conversation.Id == conversationId, ct))
            return null;

        var principal = binding.Policy == "principal_administration";
        if (!principal && binding.Policy != "conversation_grant")
            return null;

        var scopes = principal
            ? ImmutableArray.Create(nameof(ConversationAccessScope.Read))
            : binding.RequiredScopes;
        var evidence = ImmutableArray.CreateBuilder<ResourceGrantEvidence>();
        foreach (var requiredScope in scopes)
        {
            if (!Enum.TryParse<ConversationAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var eligible = principal
                ? ConversationGrantPolicy.Principal(context.ConversationAccessGrants.AsNoTracking(), actor, now.UtcDateTime)
                : ConversationGrantPolicy.Eligible(
                    context.ConversationAccessGrants.AsNoTracking(), actor, binding.Permission, scope, now.UtcDateTime);
            eligible = eligible.Where(grant => grant.ResourceId == conversationId);
            if (pinned is not null)
            {
                var original = pinned.Grants.SingleOrDefault(grant => grant.Scope == requiredScope);
                if (original is null)
                    return null;
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
                return null;

            evidence.Add(new ResourceGrantEvidence(
                requiredScope,
                grant.Id,
                grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return new ResourceAuthorizationEvidence(
            principal ? actor.TenantId : null,
            evidence.ToImmutable());
    }
}
