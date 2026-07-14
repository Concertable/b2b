using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class DealAccessor : IDealAccessor, IDealResolver
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IConcertRepository concertRepository;
    private readonly IDealModule contractModule;

    private IDeal? contract;

    public DealAccessor(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IConcertRepository concertRepository,
        IDealModule contractModule)
    {
        this.applicationRepository = applicationRepository;
        this.opportunityRepository = opportunityRepository;
        this.concertRepository = concertRepository;
        this.contractModule = contractModule;
    }

    public IDeal Contract => contract
        ?? throw new InvalidOperationException(
            "No contract resolved this scope — the operation's orchestrator must resolve the contract before a step reads it.");

    public Task<IDeal> ResolveByOpportunityIdAsync(int opportunityId) =>
        ResolveAsync(() => opportunityRepository.GetDealIdByIdAsync(opportunityId));

    public Task<IDeal> ResolveByApplicationIdAsync(int applicationId) =>
        ResolveAsync(() => applicationRepository.GetDealIdByIdAsync(applicationId));

    public Task<IDeal> ResolveByConcertIdAsync(int concertId) =>
        ResolveAsync(() => concertRepository.GetDealIdByIdAsync(concertId));

    private async Task<IDeal> ResolveAsync(Func<Task<int?>> resolveDealId)
    {
        if (contract is not null)
            return contract;

        var contractId = await resolveDealId()
            ?? throw new NotFoundException("Contract not found for this entity");

        return contract = await contractModule.GetByIdAsync(contractId)
            ?? throw new NotFoundException($"No contract with id {contractId}");
    }
}
