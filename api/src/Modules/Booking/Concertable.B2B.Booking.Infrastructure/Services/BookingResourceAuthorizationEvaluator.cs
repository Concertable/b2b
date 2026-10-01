using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingResourceAuthorizationEvaluator(
    BookingPrivilegedDbContext context,
    CommandTransactionAccessor transactions)
    : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Booking;

    public Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);

    public async Task<ResourceAuthorizationEvidence?> RequireAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Booking authorization requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."Bookings" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."BookingAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof, DateTimeOffset now, CancellationToken ct = default)
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
        int bookingId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct,
        ResourceAuthorizationEvidence? pinned = null)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return null;

        var booking = await BookingGrantPolicy.EligibleBookingResources(
                context.Bookings.AsNoTracking(), actor.TenantId, binding.Policy)
            .Where(candidate => candidate.Id == bookingId)
            .Select(candidate => new { candidate.VenueTenantId, candidate.ArtistTenantId })
            .SingleOrDefaultAsync(ct);
        if (booking is null)
            return null;

        Guid? principal = binding.Policy == "booking_grant" ? null : actor.TenantId;

        if (pinned is not null && pinned.Grants.Length != binding.RequiredScopes.Length)
            return null;

        var evidence = ImmutableArray.CreateBuilder<ResourceGrantEvidence>();
        foreach (var requiredScope in binding.RequiredScopes)
        {
            if (!Enum.TryParse<BookingAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var pinnedGrant = pinned?.Grants.SingleOrDefault(candidate => candidate.Scope == requiredScope);
            if (pinned is not null && pinnedGrant is null)
                return null;

            var selectedId = pinnedGrant?.GrantId;
            var grant = await BookingGrantPolicy.EligibleBookings(
                    context.BookingAccessGrants.AsNoTracking(), actor, binding.Permission, scope, now.UtcDateTime)
                .Where(candidate => candidate.ResourceId == bookingId
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

internal sealed class ContractResourceAuthorizationEvaluator(
    BookingPrivilegedDbContext context,
    CommandTransactionAccessor transactions)
    : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Contract;

    public Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);

    public async Task<ResourceAuthorizationEvidence?> RequireAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Contract authorization requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."Contracts" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."ContractAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return await ReadEvidenceAsync(request.Resource.Id, binding, actor, now, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof, DateTimeOffset now, CancellationToken ct = default)
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
        int contractId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct,
        ResourceAuthorizationEvidence? pinned = null)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return null;

        if (!await BookingGrantPolicy.EligibleContractResources(
                context.Contracts.AsNoTracking(), binding.Policy)
            .AnyAsync(contract => contract.Id == contractId, ct))
            return null;

        if (pinned is not null && pinned.Grants.Length != binding.RequiredScopes.Length)
            return null;

        var evidence = ImmutableArray.CreateBuilder<ResourceGrantEvidence>();
        foreach (var requiredScope in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ContractAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return null;

            var pinnedGrant = pinned?.Grants.SingleOrDefault(candidate => candidate.Scope == requiredScope);
            if (pinned is not null && pinnedGrant is null)
                return null;

            var selectedId = pinnedGrant?.GrantId;
            var grant = await BookingGrantPolicy.EligibleContracts(
                    context.ContractAccessGrants.AsNoTracking(), actor, binding.Permission, scope, now.UtcDateTime)
                .Where(candidate => candidate.ResourceId == contractId
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

        return new ResourceAuthorizationEvidence(null, evidence.ToImmutable());
    }
}