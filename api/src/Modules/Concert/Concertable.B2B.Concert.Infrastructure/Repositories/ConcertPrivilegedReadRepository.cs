using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivilegedReadRepository(
    ConcertPrivilegedDbContext context,
    IInvoicePrivilegedReadRepository invoiceReads) : IConcertPrivilegedReadRepository
{
    public Task<ConcertSummary?> GetSummaryByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        SummaryRoot(permission, actor, now)
            .Where(concert => concert.Id == id)
            .ToSummary()
            .SingleOrDefaultAsync(ct);

    public Task<ConcertOperations?> GetOperationsByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        OperationsRoot(permission, actor, now)
            .Where(concert => concert.Id == id)
            .ToOperations(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .SingleOrDefaultAsync(ct);

    public async Task<ConcertFinance?> GetFinanceByIdAsync(
        int id, TenantPermission permission, TenantPermission invoicePermission,
        MembershipSnapshot actor, DateTimeOffset now, CancellationToken ct = default)
    {
        var finance = await FinanceRoot(permission, actor, now)
            .Where(concert => concert.Id == id)
            .Select(concert => new ConcertFinance(
                concert.Id,
                concert.TicketsSold,
                concert is DoorRevenueConcert ? ((DoorRevenueConcert)concert).DoorRevenue : null,
                concert is DoorRevenueConcert,
                null,
                false))
            .SingleOrDefaultAsync(ct);
        if (finance is null)
            return null;

        var invoice = await invoiceReads.GetByConcertIdAsync(id, invoicePermission, actor, now, ct);
        return finance with { InvoiceId = invoice?.Id };
    }

    public Task<bool> IsAccessibleAsync(
        int id, TenantPermission permission, ConcertAccessScope scope, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        Root(permission, actor, now, scope).AnyAsync(concert => concert.Id == id, ct);

    public Task<bool> CanDeclareDoorRevenueByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        FinanceRoot(permission, actor, now)
            .AnyAsync(concert => concert.Id == id
                && (concert is DoorSplitConcert && ((DoorSplitConcert)concert).DoorRevenue == null
                    || concert is VersusConcert && ((VersusConcert)concert).DoorRevenue == null)
                && concert.Period.End < now.UtcDateTime
                && (concert.State == Concertable.B2B.Concert.Domain.Lifecycle.ConcertState.Draft
                    || concert.State == Concertable.B2B.Concert.Domain.Lifecycle.ConcertState.Posted), ct);

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(
        Guid venueTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(permission, actor, now)
            .Where(concert => concert.VenueTenantId == venueTenantId
                && concert.Period.End > now.UtcDateTime
                && concert.DatePosted != null)
            .OrderBy(concert => concert.Period.Start)
            .Take(5)
            .Select(concert => new ManagerConcertCard(
                concert.Id, concert.Name, concert.BannerUrl ?? concert.Artist.BannerUrl,
                concert.Period.Start, concert.Period.End, concert.Artist.Name,
                concert.TicketsSold, concert.TotalTickets,
                $"/_venue/my/concerts/concert/{concert.Id}"))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForArtistTenantIdAsync(
        Guid artistTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(permission, actor, now)
            .Where(concert => concert.ArtistTenantId == artistTenantId
                && concert.Period.End > now.UtcDateTime
                && concert.DatePosted != null)
            .OrderBy(concert => concert.Period.Start)
            .Take(5)
            .Select(concert => new ManagerConcertCard(
                concert.Id, concert.Name, concert.BannerUrl ?? concert.Artist.BannerUrl,
                concert.Period.Start, concert.Period.End, concert.Venue.Name,
                concert.TicketsSold, concert.TotalTickets,
                $"/_artist/my/concerts/concert/{concert.Id}"))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertDraftReference>> GetDraftReferencesForVenueTenantIdAsync(
        Guid venueTenantId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(permission, actor, now)
            .Where(concert => concert.VenueTenantId == venueTenantId && concert.DatePosted == null)
            .Select(concert => new ConcertDraftReference(concert.Id, concert.ApplicationId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int artistId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await SummaryRoot(permission, actor, now)
            .Where(concert => concert.ArtistId == artistId && concert.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int venueId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await SummaryRoot(permission, actor, now)
            .Where(concert => concert.VenueId == venueId && concert.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public Task<int> CountSummariesAsync(
        TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        SummaryRoot(permission, actor, now).CountAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetSummaryPageAsync(
        TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, int afterId, int take, CancellationToken ct = default)
    {
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take));

        return await SummaryRoot(permission, actor, now)
            .Where(concert => concert.Id > afterId)
            .OrderBy(concert => concert.Id)
            .Take(take)
            .ToSummary()
            .ToListAsync(ct);
    }

    private IQueryable<ConcertEntity> SummaryRoot(
        TenantPermission permission, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(permission, actor, now, ConcertAccessScope.Summary);

    private IQueryable<ConcertEntity> OperationsRoot(
        TenantPermission permission, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(permission, actor, now, ConcertAccessScope.Operations);

    private IQueryable<ConcertEntity> FinanceRoot(
        TenantPermission permission, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(permission, actor, now, ConcertAccessScope.Finance);

    private IQueryable<ConcertEntity> Root(
        TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, ConcertAccessScope scope) =>
        context.Concerts.AsNoTracking()
            .AccessibleBy(actor, permission, scope, now.UtcDateTime)
            .WhereCurrentMember(context.MembershipAuthority, actor);
}
