using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivilegedRepository : PrivilegedRepository<ConcertEntity>, IConcertPrivilegedRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public ConcertPrivilegedRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public void AddAccessGrants(IEnumerable<ConcertAccessGrant> grants) =>
        context.ConcertAccessGrants.AddRange(grants);

    public async Task<ConcertEntity?> GetByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await this.AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public Task<ConcertEntity?> GetByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Concerts.SingleOrDefaultAsync(concert => concert.BookingId == bookingId, ct);

    public Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default) =>
        context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);

    public async Task<ConcertEntity?> GetWithGrantsByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts
            .Include(concert => concert.AccessGrants)
            .SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
    }

    public async Task<ConcertAccessIdentity?> GetIdentityByIdForUpdateAsync(
        int concertId,
        CancellationToken ct = default)
    {
        await this.AcquireUpdateLockAsync(concertId, ct);
        return await context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => new ConcertAccessIdentity(
                concert.Id, concert.VenueTenantId, concert.ArtistTenantId, concert.AccessVersion))
            .SingleOrDefaultAsync(ct);
    }

    public Task<ConcertState?> GetStateByIdAsync(
        int concertId,
        CancellationToken ct = default) =>
        context.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (ConcertState?)concert.State)
            .SingleOrDefaultAsync(ct);

    public Task<bool> CanManageAsync(
        int concertId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        context.Concerts.AsNoTracking().AnyAsync(concert =>
            concert.Id == concertId
            && (concert.VenueTenantId == actor.TenantId || concert.ArtistTenantId == actor.TenantId)
            && context.ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id
                && grant.Scope == ConcertAccessScope.Operations
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId)),
            ct);

    public Task<bool> CanOperateAsync(
        int concertId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        CanOperateAsync(concertId, actor, audience, at, requiresFinance: false, ct);

    public Task<bool> CanDeclareDoorRevenueAsync(
        int concertId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        CanOperateAsync(concertId, actor, audience, at, requiresFinance: true, ct);

    public Task<bool> CanShareAsync(
        int concertId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        context.Concerts.AsNoTracking().AnyAsync(concert =>
            concert.Id == concertId
            && (concert.VenueTenantId == actor.TenantId || concert.ArtistTenantId == actor.TenantId)
            && context.ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id
                && grant.Scope == ConcertAccessScope.Summary
                && grant.Kind == ResourceGrantKind.Principal
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId)),
            ct);

    private Task<bool> CanOperateAsync(
        int concertId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        bool requiresFinance,
        CancellationToken ct) =>
        context.Concerts.AsNoTracking().AnyAsync(concert =>
            concert.Id == concertId
            && concert.VenueTenantId == actor.TenantId
            && context.ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id
                && grant.Scope == ConcertAccessScope.Operations
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId))
            && (!requiresFinance || context.ConcertAccessGrants.Any(grant =>
                grant.ResourceId == concert.Id
                && grant.Scope == ConcertAccessScope.Finance
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && audience == ResourceAudience.TenantResources
                && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId))),
            ct);

    private async Task AcquireUpdateLockAsync(int concertId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert.Concerts WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = {concertId}
            """, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert.ConcertAccessGrants WITH (UPDLOCK, HOLDLOCK)
            WHERE ResourceId = {concertId}
            """, ct);
    }
}
