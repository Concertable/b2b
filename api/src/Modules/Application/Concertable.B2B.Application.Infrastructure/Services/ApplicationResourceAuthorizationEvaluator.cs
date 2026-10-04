using System.Collections.Immutable;
using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationResourceAuthorizationEvaluator(
    ApplicationPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor)
    : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Application;

    public Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default) =>
        EvaluateAsync(request.Resource.Id, binding, actor, now, ct);

    public async Task<ResourceAuthorizationDecision> RequireAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Application authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        var ids = new[] { request.Resource.Id };
        if (binding.Permission == TenantPermission.ApplicationsDecide)
        {
            var opportunityId = await context.Applications.AsNoTracking()
                .Where(application => application.Id == request.Resource.Id)
                .Select(application => (int?)application.OpportunityId)
                .SingleOrDefaultAsync(ct);
            if (opportunityId is { } id)
                ids = await context.Applications.AsNoTracking()
                    .Where(application => application.OpportunityId == id)
                    .OrderBy(application => application.Id)
                    .Select(application => application.Id)
                    .ToArrayAsync(ct);
        }
        foreach (var id in ids)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM application."Applications" WHERE "Id" = {id} FOR UPDATE""", ct);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM application."ApplicationAccessGrants" WHERE "ResourceId" = {id} ORDER BY "Id" FOR UPDATE""", ct);
        }
        return await EvaluateAsync(request.Resource.Id, binding, actor, now, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationSnapshot snapshot,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        if (snapshot.Request.Resource.Kind != Kind || snapshot.Binding.Resource != Kind)
            return false;

        var current = await EvaluateAsync(
            snapshot.Request.Resource.Id, snapshot.Binding, snapshot.Authority.Actor, now, ct, snapshot);
        return current.IsAllowed
            && current.PrincipalTenantId == snapshot.PrincipalTenantId
            && current.Grants.Length == snapshot.Grants.Length
            && current.Grants.All(grant => snapshot.Grants.Contains(grant));
    }

    private async Task<ResourceAuthorizationDecision> EvaluateAsync(
        int applicationId,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct,
        ResourceAuthorizationSnapshot? pinned = null)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return ResourceAuthorizationDecision.Denied;

        var application = await ApplicationAuthorizationPolicy.EligibleApplications(
                context.Applications.AsNoTracking(), actor.TenantId, binding.Policy)
            .Where(candidate => candidate.Id == applicationId)
            .Select(candidate => new { candidate.VenueTenantId, candidate.ArtistTenantId })
            .SingleOrDefaultAsync(ct);
        if (application is null)
            return ResourceAuthorizationDecision.Denied;

        Guid? principal = binding.Policy == "application_grant" ? null : actor.TenantId;

        if (pinned is not null && pinned.Grants.Length != binding.RequiredScopes.Length)
            return ResourceAuthorizationDecision.Denied;

        var grants = ImmutableArray.CreateBuilder<ResourceGrantSnapshot>();
        foreach (var requiredScope in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ApplicationAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return ResourceAuthorizationDecision.Denied;

            var pinnedGrant = pinned?.Grants.SingleOrDefault(candidate => candidate.Scope == requiredScope);
            if (pinned is not null && pinnedGrant is null)
                return ResourceAuthorizationDecision.Denied;

            var selectedId = pinnedGrant?.GrantId;
            var grant = await ApplicationAuthorizationPolicy.Eligible(
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
                return ResourceAuthorizationDecision.Denied;

            grants.Add(new ResourceGrantSnapshot(
                requiredScope, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return ResourceAuthorizationDecision.Allow(principal, grants.ToImmutable());
    }
}