using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertResourceAuthorizationEvaluator(
    ConcertPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Concert;

    public Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, true, ct);

    public async Task<ResourceAuthorizationEvidence?> RequireAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Concert authorization requires an active command.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."Concerts" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."ConcertAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, true, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof, DateTimeOffset now, CancellationToken ct = default)
    {
        if (proof.Request.Resource.Kind != Kind || proof.Binding.Resource != Kind)
            return false;

        var current = await ReadEvidenceAsync(
            proof.Request.Resource.Id, proof.Binding, proof.Authority.Actor,
            now, proof.Evidence, false, ct);
        return current is not null
            && current.PrincipalTenantId == proof.Evidence.PrincipalTenantId
            && current.Grants.SequenceEqual(proof.Evidence.Grants);
    }

    private async Task<ResourceAuthorizationEvidence?> ReadEvidenceAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now,
        ResourceAuthorizationEvidence? pinned, bool requireCurrentMembership, CancellationToken ct)
    {
        if (binding.Resource != Kind)
            return null;

        var policy = ConcertAuthorizationPolicy.Concerts(
            context, binding, actor, now, pinned, requireCurrentMembership);
        if (!await context.Concerts.AsNoTracking().Where(policy).AnyAsync(concert => concert.Id == id, ct))
            return null;

        var evidence = ImmutableArray.CreateBuilder<ResourceGrantEvidence>();
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ConcertAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var original = pinned?.Grants.SingleOrDefault(grant => grant.Scope == name);
            if (pinned is not null && original is null)
                return null;

            var grant = await ConcertAuthorizationPolicy.ConcertGrants(
                    context, actor, binding.Permission, scope, now, original)
                .Where(candidate => candidate.ResourceId == id)
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
                name, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return new ResourceAuthorizationEvidence(
            ConcertAuthorizationPolicy.PrincipalTenantId(binding.Policy, actor),
            evidence.ToImmutable());
    }
}
