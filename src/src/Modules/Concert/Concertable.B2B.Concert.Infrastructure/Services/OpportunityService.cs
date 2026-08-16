using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class OpportunityService : IOpportunityService
{
    private readonly IOpportunityRepository repository;
    private readonly IPublicOpportunityRepository publicRepository;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly IOpportunitySyncer syncer;
    private readonly IOpportunityMapper mapper;
    private readonly ITenantContext tenantContext;
    private readonly IUnitOfWorkBehavior uowBehavior;
    private readonly IArtistModule artistModule;
    private readonly TimeProvider timeProvider;

    public OpportunityService(
        IOpportunityRepository repository,
        IPublicOpportunityRepository publicRepository,
        IVenueModule venueModule,
        IDealModule dealModule,
        IOpportunitySyncer syncer,
        IOpportunityMapper mapper,
        ITenantContext tenantContext,
        IUnitOfWorkBehavior uowBehavior,
        IArtistModule artistModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.syncer = syncer;
        this.mapper = mapper;
        this.tenantContext = tenantContext;
        this.uowBehavior = uowBehavior;
        this.artistModule = artistModule;
        this.timeProvider = timeProvider;
    }

    public async Task<OpportunityDto> CreateAsync(OpportunityRequest request)
    {
        var venueId = await venueModule.GetVenueIdForCurrentTenantAsync()
            ?? throw new NotFoundException("Venue not found for current user");

        var opportunity = await uowBehavior.ExecuteAsync(async () =>
        {
            var dealId = await dealModule.CreateAsync(request.Deal);
            var entity = OpportunityEntity.Create(
                venueId,
                new DateRange(request.StartDate, request.EndDate),
                dealId,
                request.Genres);
            await repository.AddAsync(entity);
            return entity;
        });

        var saved = await repository.GetByIdAsync(opportunity.Id)
            ?? throw new NotFoundException("Opportunity not found after save");
        return await mapper.ToDtoAsync(saved);
    }

    public async Task CreateMultipleAsync(IEnumerable<OpportunityRequest> requests)
    {
        var requestList = requests.ToList();
        var venueId = await venueModule.GetVenueIdForCurrentTenantAsync()
            ?? throw new NotFoundException("Venue not found for current user");

        await uowBehavior.ExecuteAsync(async () =>
        {
            foreach (var request in requestList)
            {
                var dealId = await dealModule.CreateAsync(request.Deal);
                var opportunity = OpportunityEntity.Create(
                    venueId,
                    new DateRange(request.StartDate, request.EndDate),
                    dealId,
                    request.Genres);
                await repository.AddAsync(opportunity);
            }
        });
    }

    public async Task<IPagination<OpportunityDto>> GetActiveByVenueIdAsync(int id, IPageParams pageParams)
    {
        var opportunities = await publicRepository.GetActiveByVenueIdAsync(id, pageParams);
        return await mapper.ToDtosAsync(opportunities);
    }

    public async Task<IEnumerable<OpportunityDto>> GetActiveByVenueIdAsync(int venueId)
    {
        var opportunities = await publicRepository.GetActiveByVenueIdAsync(venueId);
        return await mapper.ToDtosAsync(opportunities);
    }

    public async Task<IEnumerable<OpportunityDto>> UpdateAsync(int venueId, IEnumerable<OpportunityRequest> desired)
    {
        var ownedVenueId = await venueModule.GetVenueIdForCurrentTenantAsync()
            ?? throw new NotFoundException("Venue not found for current user");

        if (ownedVenueId != venueId)
            throw new ForbiddenException("You do not own this venue");

        /* Read tracked through the writing context: the syncer mutates these entities, and the
           read-only public projection's no-tracking context would silently drop those updates. */
        var current = await repository.GetActiveByVenueIdAsync(venueId);

        await uowBehavior.ExecuteAsync(() => syncer.SyncAsync(venueId, current, desired));

        var updated = await publicRepository.GetActiveByVenueIdAsync(venueId);
        return await mapper.ToDtosAsync(updated);
    }

    public async Task<OpportunityDto> GetByIdAsync(int id)
    {
        var opportunity = await repository.GetByIdAsync(id)
            .OrNotFound();
        return await mapper.ToDtoAsync(opportunity);
    }

    public async Task<Guid?> GetOwnerByIdAsync(int id)
    {
        return await repository.GetOwnerByIdAsync(id);
    }

    public async Task<bool> OwnsOpportunityAsync(int opportunityId)
    {
        if (tenantContext.TenantId is not { } tenant)
            return false;

        var ownerTenantId = await repository.GetTenantIdByIdAsync(opportunityId);
        return ownerTenantId == tenant;
    }

    public async Task<bool> OwnsOpportunityByApplicationIdAsync(int applicationId)
    {
        if (tenantContext.TenantId is not { } tenant)
            return false;

        var opportunity = await repository.GetByApplicationIdAsync(applicationId);
        return opportunity?.TenantId == tenant;
    }

    public async Task<IReadOnlyList<VenueOpenOpportunity>> GetOpenForCurrentVenueAsync()
    {
        var venueId = await venueModule.GetVenueIdForCurrentTenantAsync()
            ?? throw new ForbiddenException("You must have a Venue account");
        var rows = await repository.GetOpenWithCountsByVenueIdAsync(venueId);
        var deals = await GetDealsAsync(rows);
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        return rows.Select(row => new VenueOpenOpportunity(
            ToSummary(row, deals[row.DealId]),
            row.ApplicationCount,
            Math.Max(0, (row.StartDate.Date.AddDays(-7) - today).Days))).ToList();
    }

    public async Task<IReadOnlyList<RecommendedOpportunity>> GetRecommendedForCurrentArtistAsync()
    {
        var artistId = await artistModule.GetIdForCurrentTenantAsync()
            ?? throw new ForbiddenException("You must have an Artist account");
        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var rows = await publicRepository.GetRecommendedAsync(artistId, artistGenres);
        var deals = await GetDealsAsync(rows);

        return rows.Select(row => new RecommendedOpportunity(
            row.Id,
            row.VenueId,
            row.VenueName,
            row.County,
            row.Town,
            row.StartDate,
            row.EndDate,
            row.Genres,
            deals[row.DealId],
            CalculateFitScore(row.Genres, artistGenres),
            $"/_artist/find/venue/{row.VenueId}"))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<int, IDeal>> GetDealsAsync(IReadOnlyList<OpportunityListRow> rows) =>
        (await dealModule.GetByIdsAsync(rows.Select(row => row.DealId).Distinct()))
            .ToDictionary(deal => deal.Id);

    private static ManagerOpportunitySummary ToSummary(OpportunityListRow row, IDeal deal) =>
        new(row.Id, row.VenueId, row.VenueName, row.StartDate, row.EndDate, row.Genres, deal);

    private static int CalculateFitScore(IReadOnlyList<Genre> opportunityGenres, IReadOnlySet<Genre> artistGenres)
    {
        if (opportunityGenres.Count == 0)
            return 100;

        var matchingGenres = opportunityGenres.Count(artistGenres.Contains);
        return (int)Math.Round(matchingGenres * 100d / opportunityGenres.Count);
    }
}
