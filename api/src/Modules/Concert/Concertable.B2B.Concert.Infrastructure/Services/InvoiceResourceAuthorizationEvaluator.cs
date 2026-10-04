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

    public Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        EvaluateAsync(request.Resource.Id, binding, actor, now, null, true, ct);

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
        return await EvaluateAsync(request.Resource.Id, binding, actor, now, null, true, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationSnapshot snapshot, DateTimeOffset now, CancellationToken ct = default)
    {
        if (snapshot.Request.Resource.Kind != Kind || snapshot.Binding.Resource != Kind)
            return false;

        var current = await EvaluateAsync(
            snapshot.Request.Resource.Id, snapshot.Binding, snapshot.Authority.Actor,
            now, snapshot, false, ct);
        return current.IsAllowed
            && current.PrincipalTenantId == snapshot.PrincipalTenantId
            && current.Grants.SequenceEqual(snapshot.Grants);
    }

    private async Task<ResourceAuthorizationDecision> EvaluateAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now,
        ResourceAuthorizationSnapshot? pinned, bool requireCurrentMembership, CancellationToken ct)
    {
        if (binding.Resource != Kind)
            return ResourceAuthorizationDecision.Denied;

        var policy = ConcertGrantPolicy.Invoices(
            context, binding, actor, now, pinned, requireCurrentMembership);
        if (!await context.Invoices.AsNoTracking().Where(policy).AnyAsync(invoice => invoice.Id == id, ct))
            return ResourceAuthorizationDecision.Denied;

        var grants = ImmutableArray.CreateBuilder<ResourceGrantSnapshot>();
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<InvoiceAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                return ResourceAuthorizationDecision.Denied;

            var original = pinned?.Grants.SingleOrDefault(grant => grant.Scope == name);
            if (pinned is not null && original is null)
                return ResourceAuthorizationDecision.Denied;

            var grant = await ConcertGrantPolicy.InvoiceGrants(
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
                return ResourceAuthorizationDecision.Denied;

            grants.Add(new ResourceGrantSnapshot(
                name, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return ResourceAuthorizationDecision.Allow(null, grants.ToImmutable());
    }
}
