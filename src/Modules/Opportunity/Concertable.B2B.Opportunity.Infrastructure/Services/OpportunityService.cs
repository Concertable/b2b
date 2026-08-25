using Concertable.B2B.Opportunity.Application.Errors;
using Concertable.B2B.Opportunity.Application.Mappers;
using Concertable.B2B.Opportunity.Contracts.Errors;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;
using Reunion;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityService : IOpportunityService
{
    private readonly IOpportunityRepository repository;
    private readonly IOpportunityReadRepository readRepository;
    private readonly IVenueModule venueModule;
    private readonly IDealModule dealModule;
    private readonly IOpportunitySyncer syncer;
    private readonly ITenantContext tenantContext;
    private readonly IUnitOfWorkBehavior uowBehavior;

    public OpportunityService(
        IOpportunityRepository repository,
        IOpportunityReadRepository readRepository,
        IVenueModule venueModule,
        IDealModule dealModule,
        IOpportunitySyncer syncer,
        ITenantContext tenantContext,
        IUnitOfWorkBehavior uowBehavior)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.venueModule = venueModule;
        this.dealModule = dealModule;
        this.syncer = syncer;
        this.tenantContext = tenantContext;
        this.uowBehavior = uowBehavior;
    }

    public async Task<Result<OpportunityDto, OpportunityMutationError>> CreateAsync(OpportunityRequest request)
    {
        var venue = await venueModule.GetCurrentIdAsync();
        if (!venue.TryGetValue(out var venueId))
            return new OpportunityMutationError.VenueNotFound();

        var creation = await uowBehavior.ExecuteAsync(async () =>
        {
            var deal = await CreateDealAsync(request.Deal);
            return await deal.BindAsync(async dealId =>
            {
                var entity = OpportunityEntity.Create(
                    venueId,
                    new DateRange(request.StartDate, request.EndDate),
                    dealId,
                    request.Genres.ToHashSet());
                await repository.AddAsync(entity);
                return Result.Success<OpportunityEntity, OpportunityMutationError>(entity);
            });
        });

        return creation.Map(opportunity => opportunity.ToDto());
    }

    public async Task<Option<OpportunityDto>> GetAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        (await readRepository.GetByIdAsync(opportunityId, ct))
            .ToOption()
            .Map(opportunity => opportunity.ToDto());

    public async Task<IReadOnlyList<OpportunityDto>> GetAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        (await readRepository.GetByIdsAsync(opportunityIds, ct))
            .Select(opportunity => opportunity.ToDto())
            .ToList();

    public async Task<Option<OpportunityDto>> GetOpenAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        (await readRepository.GetOpenByIdAsync(opportunityId, ct))
            .ToOption()
            .Map(opportunity => opportunity.ToDto());

    public async Task<UnitResult<FillOpportunityError>> FillAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        if (!await repository.TryFillAsync(opportunityId, venueTenantId, ct))
            return new FillOpportunityError.Unavailable(opportunityId);

        return new Success();
    }

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        readRepository.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        readRepository.GetOpenCountAsync(venueTenantId, ct);

    public async Task<IReadOnlyList<OpportunityDto>> GetOpenByVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        (await repository.GetOpenByVenueTenantIdAsync(venueTenantId, ct))
            .Select(opportunity => opportunity.ToDto())
            .ToList();

    public async Task<IReadOnlyList<OpportunityDto>> GetRecommendedAsync(
        IReadOnlyCollection<int> excludedOpportunityIds,
        IReadOnlySet<Genre> genres,
        CancellationToken ct = default) =>
        (await readRepository.GetMatchCandidatesAsync(excludedOpportunityIds, genres, ct))
            .Select(opportunity => opportunity.ToDto())
            .ToList();

    public async Task<UnitResult<OpportunityMutationError>> CreateMultipleAsync(IEnumerable<OpportunityRequest> requests)
    {
        var requestList = requests.ToList();
        var venue = await venueModule.GetCurrentIdAsync();
        if (!venue.TryGetValue(out var venueId))
            return new OpportunityMutationError.VenueNotFound();

        var validation = ValidateDeals(requestList.Select(request => request.Deal));
        if (validation.IsFailure)
            return validation;

        await uowBehavior.ExecuteAsync(async () =>
        {
            foreach (var request in requestList)
            {
                var dealId = await CreatePrevalidatedDealAsync(request.Deal);
                var opportunity = OpportunityEntity.Create(
                    venueId,
                    new DateRange(request.StartDate, request.EndDate),
                    dealId,
                    request.Genres.ToHashSet());
                await repository.AddAsync(opportunity);
            }
        });

        return new Success();
    }

    public async Task<IPagination<OpportunityDto>> GetActiveByVenueIdAsync(int id, IPageParams pageParams)
    {
        var opportunities = await readRepository.GetActiveByVenueIdAsync(id, pageParams);
        return opportunities.Map(opportunity => opportunity.ToDto());
    }

    public async Task<IReadOnlyList<OpportunityDto>> GetActiveByVenueIdAsync(int venueId)
    {
        var opportunities = await readRepository.GetActiveByVenueIdAsync(venueId);
        return opportunities.Select(opportunity => opportunity.ToDto()).ToList();
    }

    public async Task<Result<IReadOnlyList<OpportunityDto>, OpportunityMutationError>> UpdateAsync(
        int venueId,
        IEnumerable<OpportunityRequest> desired)
    {
        var venue = await venueModule.GetCurrentIdAsync();
        if (!venue.TryGetValue(out var currentVenueId))
            return new OpportunityMutationError.VenueNotFound();

        if (currentVenueId != venueId)
            return new OpportunityMutationError.VenueForbidden();

        var desiredList = desired.ToList();
        var validation = ValidateDeals(desiredList.Select(request => request.Deal));
        if (validation.TryGetError(out var error))
            return error;

        var current = await repository.GetActiveByVenueIdAsync(venueId);

        await uowBehavior.ExecuteAsync(() => syncer.SyncAsync(venueId, current, desiredList));

        var updated = await readRepository.GetActiveByVenueIdAsync(venueId);
        return updated.Select(opportunity => opportunity.ToDto()).ToList();
    }

    public Task<Result<OpportunityDto, OpportunityError>> GetByIdAsync(int id) =>
        repository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (OpportunityError)new OpportunityError.NotFound(id))
            .Map(opportunity => opportunity.ToDto());

    public async Task<bool> OwnsOpportunityAsync(int opportunityId)
    {
        if (tenantContext.TenantId is not { } tenant)
            return false;

        var ownerTenantId = await repository.GetTenantIdByIdAsync(opportunityId);
        return ownerTenantId == tenant;
    }

    private UnitResult<OpportunityMutationError> ValidateDeals(IEnumerable<DealDto> deals)
    {
        foreach (var deal in deals)
        {
            var validation = dealModule.Validate(deal)
                .MapError<OpportunityMutationError>(
                    errors => new OpportunityMutationError.InvalidDeal(errors));
            if (validation.IsFailure)
                return validation;
        }

        return new Success();
    }

    private async Task<Result<int, OpportunityMutationError>> CreateDealAsync(DealDto deal) =>
        (await dealModule.CreateAsync(deal))
            .MapError<OpportunityMutationError>(
                error => error.Match<OpportunityMutationError>(
                    invalid => new OpportunityMutationError.InvalidDeal(invalid.Errors)));

    private async Task<int> CreatePrevalidatedDealAsync(DealDto deal)
    {
        var result = await dealModule.CreateAsync(deal);
        if (result.TryGetValue(out var dealId))
            return dealId;

        throw new InvalidOperationException("Deal creation failed after successful validation.");
    }
}
