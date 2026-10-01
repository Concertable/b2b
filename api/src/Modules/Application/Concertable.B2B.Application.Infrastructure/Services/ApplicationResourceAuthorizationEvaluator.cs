using System.Collections.Immutable;
using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationResourceAuthorizationEvaluator(
    ApplicationPrivilegedDbContext context,
    CommandTransactionAccessor transactions)
    : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Application;

    public Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default) =>
        ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);

    public async Task<ResourceAuthorizationEvidence?> RequireAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Application authorization requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM application."Applications" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM application."ApplicationAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        if (proof.Request.Resource.Kind != Kind || proof.Binding.Resource != Kind)
            return false;

        var current = await ReadEvidenceAsync(
            proof.Request.Resource.Id, proof.Binding, proof.Authority.Actor, now, ct, proof.Evidence);
        return current is not null
            && current.PrincipalTenantId == proof.Evidence.PrincipalTenantId
            && current.Grants.Length == proof.Evidence.Grants.Length
            && current.Grants.All(grant => proof.Evidence.Grants.Contains(grant));
    }

    private async Task<ResourceAuthorizationEvidence?> ReadEvidenceAsync(
        int applicationId,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct,
        ResourceAuthorizationEvidence? pinned = null)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return null;

        var application = await ApplicationGrantPolicy.EligibleApplications(
                context.Applications.AsNoTracking(), actor.TenantId, binding.Policy)
            .Where(candidate => candidate.Id == applicationId)
            .Select(candidate => new { candidate.VenueTenantId, candidate.ArtistTenantId })
            .SingleOrDefaultAsync(ct);
        if (application is null)
            return null;

        Guid? principal = binding.Policy == "application_grant" ? null : actor.TenantId;

        if (pinned is not null && pinned.Grants.Length != binding.RequiredScopes.Length)
            return null;

        var evidence = ImmutableArray.CreateBuilder<ResourceGrantEvidence>();
        foreach (var requiredScope in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ApplicationAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var pinnedGrant = pinned?.Grants.SingleOrDefault(candidate => candidate.Scope == requiredScope);
            if (pinned is not null && pinnedGrant is null)
                return null;

            var selectedId = pinnedGrant?.GrantId;
            var grant = await ApplicationGrantPolicy.Eligible(
                    context.ApplicationAccessGrants.AsNoTracking(), actor, binding.Permission, scope, now.UtcDateTime)
                .Where(candidate => candidate.ResourceId == applicationId
                    && (selectedId == null || candidate.Id == selectedId))
                .OrderBy(candidate => candidate.Id)
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
                requiredScope, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return new ResourceAuthorizationEvidence(principal, evidence.ToImmutable());
    }
}