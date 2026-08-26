using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class DealAccessor : IDealAccessor, IDealResolver
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IConcertRepository concertRepository;
    private readonly IDealModule dealModule;

    private DealDto? deal;

    public DealAccessor(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IConcertRepository concertRepository,
        IDealModule dealModule)
    {
        this.applicationRepository = applicationRepository;
        this.opportunityRepository = opportunityRepository;
        this.concertRepository = concertRepository;
        this.dealModule = dealModule;
    }

    public DealDto Deal => deal
        ?? throw new InvalidOperationException(
            "No deal resolved this scope — the operation's orchestrator must resolve the deal before a step reads it.");

    public Task<DealDto> ResolveByOpportunityIdAsync(int opportunityId) =>
        ResolveAsync(() => opportunityRepository.GetDealIdByIdAsync(opportunityId));

    public Task<DealDto> ResolveByApplicationIdAsync(int applicationId) =>
        ResolveAsync(() => applicationRepository.GetDealIdByIdAsync(applicationId));

    public Task<DealDto> ResolveByConcertIdAsync(int concertId) =>
        ResolveAsync(() => concertRepository.GetDealIdByIdAsync(concertId));

    private async Task<DealDto> ResolveAsync(Func<Task<int?>> resolveDealId)
    {
        if (deal is not null)
            return deal;

        var dealId = await resolveDealId()
            ?? throw new NotFoundException("Deal not found for this entity");

        var dealOption = await dealModule.GetByIdAsync(dealId);
        return deal = dealOption.Match(
            value => value,
            () => throw new InvalidOperationException($"Entity references missing deal {dealId}."));
    }
}
