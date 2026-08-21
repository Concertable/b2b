using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;
using Reunion;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class OpportunityService : IOpportunityService
{
    private readonly IOpportunityRepository repository;
    private readonly IOpportunityReadRepository readRepository;
    private readonly IVenueReadModelRepository venueRepository;
    private readonly IDealModule dealModule;
    private readonly IOpportunitySyncer syncer;
    private readonly IOpportunityMapper mapper;
    private readonly ITenantContext tenantContext;
    private readonly IUnitOfWorkBehavior uowBehavior;

    public OpportunityService(
        IOpportunityRepository repository,
        IOpportunityReadRepository readRepository,
        IVenueReadModelRepository venueRepository,
        IDealModule dealModule,
        IOpportunitySyncer syncer,
        IOpportunityMapper mapper,
        ITenantContext tenantContext,
        IUnitOfWorkBehavior uowBehavior)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.venueRepository = venueRepository;
        this.dealModule = dealModule;
        this.syncer = syncer;
        this.mapper = mapper;
        this.tenantContext = tenantContext;
        this.uowBehavior = uowBehavior;
    }

    public async Task<Result<OpportunityDto, OpportunityMutationError>> CreateAsync(OpportunityRequest request)
    {
        var venue = await GetActiveTenantVenueAsync();
        if (venue is null)
            return new OpportunityMutationError.VenueNotFound();

        var creation = await uowBehavior.ExecuteAsync(async () =>
        {
            var deal = await CreateDealAsync(request.Deal);
            return await deal.BindAsync(async dealId =>
            {
                var entity = OpportunityEntity.Create(
                    venue.Id,
                    new DateRange(request.StartDate, request.EndDate),
                    dealId,
                    request.Genres);
                entity.Venue = venue;
                await repository.AddAsync(entity);
                return Result.Success<OpportunityEntity, OpportunityMutationError>(entity);
            });
        });

        return await creation.MapAsync(mapper.ToDtoAsync);
    }

    public async Task<UnitResult<OpportunityMutationError>> CreateMultipleAsync(IEnumerable<OpportunityRequest> requests)
    {
        var requestList = requests.ToList();
        var venue = await GetActiveTenantVenueAsync();
        if (venue is null)
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
                    venue.Id,
                    new DateRange(request.StartDate, request.EndDate),
                    dealId,
                    request.Genres);
                opportunity.Venue = venue;
                await repository.AddAsync(opportunity);
            }
        });

        return new Success();
    }

    public async Task<IPagination<OpportunityDto>> GetActiveByVenueIdAsync(int id, IPageParams pageParams)
    {
        var opportunities = await readRepository.GetActiveByVenueIdAsync(id, pageParams);
        return await mapper.ToDtosAsync(opportunities);
    }

    public async Task<IReadOnlyList<OpportunityDto>> GetActiveByVenueIdAsync(int venueId)
    {
        var opportunities = await readRepository.GetActiveByVenueIdAsync(venueId);
        return await mapper.ToDtosAsync(opportunities);
    }

    public async Task<Result<IReadOnlyList<OpportunityDto>, OpportunityMutationError>> UpdateAsync(
        int venueId,
        IEnumerable<OpportunityRequest> desired)
    {
        var venue = await GetActiveTenantVenueAsync();
        if (venue is null)
            return new OpportunityMutationError.VenueNotFound();

        if (venue.Id != venueId)
            return new OpportunityMutationError.VenueForbidden();

        var desiredList = desired.ToList();
        var validation = ValidateDeals(desiredList.Select(request => request.Deal));
        if (validation.TryGetError(out var error))
            return error;

        /* Read tracked through the writing context: the syncer mutates these entities, and the
           read-only public projection's no-tracking context would silently drop those updates. */
        var current = await repository.GetActiveByVenueIdAsync(venueId);

        await uowBehavior.ExecuteAsync(() => syncer.SyncAsync(venueId, current, desiredList));

        var updated = await readRepository.GetActiveByVenueIdAsync(venueId);
        return new Success<IReadOnlyList<OpportunityDto>>(
            await mapper.ToDtosAsync(updated));
    }

    public Task<Result<OpportunityDto, OpportunityError>> GetByIdAsync(int id) =>
        repository.GetByIdAsync(id)
            .ToOption()
            .OrFailure(() => (OpportunityError)new OpportunityError.NotFound(id))
            .MapAsync(mapper.ToDtoAsync);

    public async Task<Option<Guid>> GetOwnerByIdAsync(int id) =>
        (await repository.GetOwnerByIdAsync(id)).ToOption();

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

    private async Task<VenueReadModel?> GetActiveTenantVenueAsync(
        CancellationToken ct = default) =>
        tenantContext.TenantId is { } tenantId
            ? await venueRepository.GetByTenantIdAsync(tenantId, ct)
            : null;

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
