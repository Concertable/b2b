using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingResourceAuthorizationEvaluator(
    BookingPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor)
    : IResourceAuthorizationEvaluator
{
    public ResourceKind Kind => ResourceKind.Booking;

    public Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        EvaluateAsync(request.Resource.Id, binding, actor, now, ct);

    public async Task<ResourceAuthorizationDecision> RequireAsync(
        AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Booking authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."Bookings" WHERE "Id" = {request.Resource.Id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."BookingAccessGrants" WHERE "ResourceId" = {request.Resource.Id} ORDER BY "Id" FOR UPDATE""", ct);
        return await EvaluateAsync(request.Resource.Id, binding, actor, now, ct);
    }

    public async Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationSnapshot snapshot, DateTimeOffset now, CancellationToken ct = default)
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
        int bookingId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct,
        ResourceAuthorizationSnapshot? pinned = null)
    {
        if (binding.Resource != Kind || actor.AudienceFor(binding.Permission) == ResourceAudience.None)
            return ResourceAuthorizationDecision.Denied;

        var booking = await BookingAuthorizationPolicy.EligibleBookingResources(
                context.Bookings.AsNoTracking(), actor.TenantId, binding.Policy)
            .Where(candidate => candidate.Id == bookingId)
            .Select(candidate => new { candidate.VenueTenantId, candidate.ArtistTenantId })
            .SingleOrDefaultAsync(ct);
        if (booking is null)
            return ResourceAuthorizationDecision.Denied;

        Guid? principal = binding.Policy == "booking_grant" ? null : actor.TenantId;

        if (pinned is not null && pinned.Grants.Length != binding.RequiredScopes.Length)
            return ResourceAuthorizationDecision.Denied;

        var grants = ImmutableArray.CreateBuilder<ResourceGrantSnapshot>();
        foreach (var requiredScope in binding.RequiredScopes)
        {
            if (!Enum.TryParse<BookingAccessScope>(requiredScope, false, out var scope)
                || !Enum.IsDefined(scope))
                return ResourceAuthorizationDecision.Denied;

            var pinnedGrant = pinned?.Grants.SingleOrDefault(candidate => candidate.Scope == requiredScope);
            if (pinned is not null && pinnedGrant is null)
                return ResourceAuthorizationDecision.Denied;

            var selectedId = pinnedGrant?.GrantId;
            var grant = await BookingAuthorizationPolicy.EligibleBookings(
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
                return ResourceAuthorizationDecision.Denied;

            grants.Add(new ResourceGrantSnapshot(
                requiredScope, grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero),
                grant.ValidUntil is { } until ? new DateTimeOffset(until, TimeSpan.Zero) : null));
        }

        return ResourceAuthorizationDecision.Allow(principal, grants.ToImmutable());
    }
}
