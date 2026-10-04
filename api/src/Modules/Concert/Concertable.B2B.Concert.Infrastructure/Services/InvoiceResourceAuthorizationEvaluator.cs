using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceResourceAuthorizationEvaluator(
    ConcertPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor) : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Invoice;

    public async Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        ResourceAuthorizationDecision.From(await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, true, ct));

    public async Task<ResourceAuthorizationDecision> RequireAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Invoice authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."Invoices" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."InvoiceAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return ResourceAuthorizationDecision.From(await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, null, true, ct));
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

        var policy = ConcertAuthorizationPolicy.Invoices(
            context, binding, actor, now, pinned, requireCurrentMembership);
        if (!await context.Invoices.AsNoTracking().Where(policy).AnyAsync(invoice => invoice.Id == id, ct))
            return null;

        var grants = ImmutableArray.CreateBuilder<ResourceGrantSnapshot>();
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<InvoiceAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var original = pinned?.Grants.SingleOrDefault(grant => grant.Scope == name);
            if (pinned is not null && original is null)
                return null;

            var grant = await ConcertAuthorizationPolicy.InvoiceGrants(
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

            grants.Add(new ResourceGrantSnapshot(
                name, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return new ResourceAuthorizationEvidence(null, grants.ToImmutable());
    }
}
