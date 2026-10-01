using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertPrivateReadRepository(
    ConcertPrivilegedDbContext context,
    InvoicePrivateReadRepository invoiceReads) : IConcertPrivateReadRepository
{
    public Task<ConcertSummary?> GetSummaryByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        SummaryRoot(binding, actor, now)
            .Where(concert => concert.Id == id)
            .ToSummary()
            .SingleOrDefaultAsync(ct);

    public Task<ConcertOperations?> GetOperationsByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        OperationsRoot(binding, actor, now)
            .Where(concert => concert.Id == id)
            .ToOperations(
                context.ConcertRatingProjections,
                context.ArtistRatingProjections,
                context.VenueRatingProjections)
            .SingleOrDefaultAsync(ct);

    public async Task<ConcertFinance?> GetFinanceByIdAsync(
        int id, ResourcePolicyBinding binding, ResourcePolicyBinding invoiceBinding,
        MembershipSnapshot actor, DateTimeOffset now, CancellationToken ct = default)
    {
        var finance = await FinanceRoot(binding, actor, now)
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

        var invoice = await invoiceReads.GetByConcertIdAsync(id, invoiceBinding, actor, now, ct);
        return finance with { InvoiceId = invoice?.Id };
    }

    public Task<bool> CanDeclareDoorRevenueByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        FinanceRoot(binding, actor, now)
            .OfType<DoorRevenueConcert>()
            .AnyAsync(concert => concert.Id == id
                && concert.DoorRevenue == null
                && concert.Period.End < now.UtcDateTime
                && (concert.State == Concertable.B2B.Concert.Domain.Lifecycle.ConcertState.Draft
                    || concert.State == Concertable.B2B.Concert.Domain.Lifecycle.ConcertState.Posted), ct);

    public async Task<IReadOnlyList<ManagerConcertCard>> GetUpcomingCardsForVenueTenantIdAsync(
        Guid venueTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(binding, actor, now)
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
        Guid artistTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(binding, actor, now)
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
        Guid venueTenantId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await OperationsRoot(binding, actor, now)
            .Where(concert => concert.VenueTenantId == venueTenantId && concert.DatePosted == null)
            .Select(concert => new ConcertDraftReference(concert.Id, concert.ApplicationId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int artistId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await SummaryRoot(binding, actor, now)
            .Where(concert => concert.ArtistId == artistId && concert.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int venueId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        await SummaryRoot(binding, actor, now)
            .Where(concert => concert.VenueId == venueId && concert.DatePosted == null)
            .ToSummary()
            .ToListAsync(ct);

    public Task<int> CountSummariesAsync(
        ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        SummaryRoot(binding, actor, now).CountAsync(ct);

    public async Task<IReadOnlyList<ConcertSummary>> GetSummaryPageAsync(
        ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, int afterId, int take, CancellationToken ct = default)
    {
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take));

        return await SummaryRoot(binding, actor, now)
            .Where(concert => concert.Id > afterId)
            .OrderBy(concert => concert.Id)
            .Take(take)
            .ToSummary()
            .ToListAsync(ct);
    }

    private IQueryable<ConcertEntity> SummaryRoot(
        ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(binding, actor, now, ResourceFacet.Summary);

    private IQueryable<ConcertEntity> OperationsRoot(
        ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(binding, actor, now, ResourceFacet.Operations);

    private IQueryable<ConcertEntity> FinanceRoot(
        ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now) =>
        Root(binding, actor, now, ResourceFacet.Finance);

    private IQueryable<ConcertEntity> Root(
        ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, ResourceFacet facet) =>
        binding.Facet == facet
            ? context.Concerts.AsNoTracking().Where(
                ConcertAuthorizationPolicy.Concerts(context, binding, actor, now))
            : context.Concerts.AsNoTracking().Where(concert => false);
}
