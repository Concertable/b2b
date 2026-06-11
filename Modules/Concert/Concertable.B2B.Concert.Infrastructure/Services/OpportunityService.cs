using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class OpportunityService : IOpportunityService
{
    private readonly IOpportunityRepository repository;
    private readonly IPublicOpportunityRepository publicRepository;
    private readonly IVenueModule venueModule;
    private readonly IContractModule contractModule;
    private readonly IOpportunitySyncer syncer;
    private readonly IOpportunityMapper mapper;
    private readonly ICurrentUser currentUser;
    private readonly IUnitOfWorkBehavior uowBehavior;

    public OpportunityService(
        IOpportunityRepository repository,
        IPublicOpportunityRepository publicRepository,
        IVenueModule venueModule,
        IContractModule contractModule,
        IOpportunitySyncer syncer,
        IOpportunityMapper mapper,
        ICurrentUser currentUser,
        IUnitOfWorkBehavior uowBehavior)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.venueModule = venueModule;
        this.contractModule = contractModule;
        this.syncer = syncer;
        this.mapper = mapper;
        this.currentUser = currentUser;
        this.uowBehavior = uowBehavior;
    }

    public async Task<OpportunityDto> CreateAsync(OpportunityRequest request)
    {
        var venueId = await venueModule.GetVenueIdByUserIdAsync(currentUser.GetId())
            ?? throw new NotFoundException("Venue not found for current user");

        var opportunity = await uowBehavior.ExecuteAsync(async () =>
        {
            var contractId = await contractModule.CreateAsync(request.Contract);
            var entity = OpportunityEntity.Create(
                venueId,
                new DateRange(request.StartDate, request.EndDate),
                contractId,
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
        var venueId = await venueModule.GetVenueIdByUserIdAsync(currentUser.GetId())
            ?? throw new NotFoundException("Venue not found for current user");

        await uowBehavior.ExecuteAsync(async () =>
        {
            foreach (var request in requestList)
            {
                var contractId = await contractModule.CreateAsync(request.Contract);
                var opportunity = OpportunityEntity.Create(
                    venueId,
                    new DateRange(request.StartDate, request.EndDate),
                    contractId,
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
        var ownedVenueId = await venueModule.GetVenueIdByUserIdAsync(currentUser.GetId())
            ?? throw new NotFoundException("Venue not found for current user");

        if (ownedVenueId != venueId)
            throw new ForbiddenException("You do not own this venue");

        var current = await publicRepository.GetActiveByVenueIdAsync(venueId);

        await uowBehavior.ExecuteAsync(() => syncer.SyncAsync(venueId, current, desired));

        var updated = await publicRepository.GetActiveByVenueIdAsync(venueId);
        return await mapper.ToDtosAsync(updated);
    }

    public async Task<OpportunityDto> GetByIdAsync(int id)
    {
        var opportunity = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Concert Opportunity not found");
        return await mapper.ToDtoAsync(opportunity);
    }

    public async Task<Guid?> GetOwnerByIdAsync(int id)
    {
        return await repository.GetOwnerByIdAsync(id);
    }

    public async Task<bool> OwnsOpportunityAsync(int opportunityId)
    {
        var opportunity = await repository.GetWithVenueByIdAsync(opportunityId);
        return opportunity?.Venue?.UserId == currentUser.GetId();
    }

    public async Task<bool> OwnsOpportunityByApplicationIdAsync(int applicationId)
    {
        var opportunity = await repository.GetByApplicationIdAsync(applicationId);
        return opportunity?.Venue?.UserId == currentUser.GetId();
    }
}
