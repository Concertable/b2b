using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractAccessor : IContractAccessor, IContractResolver
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IConcertRepository concertRepository;
    private readonly IContractModule contractModule;

    private IContract? contract;

    public ContractAccessor(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IConcertRepository concertRepository,
        IContractModule contractModule)
    {
        this.applicationRepository = applicationRepository;
        this.opportunityRepository = opportunityRepository;
        this.concertRepository = concertRepository;
        this.contractModule = contractModule;
    }

    public IContract Contract => contract
        ?? throw new InvalidOperationException(
            "No contract resolved this scope — the operation's orchestrator must resolve the contract before a step reads it.");

    public Task<IContract> ResolveByOpportunityIdAsync(int opportunityId) =>
        ResolveAsync(() => opportunityRepository.GetContractIdByIdAsync(opportunityId));

    public Task<IContract> ResolveByApplicationIdAsync(int applicationId) =>
        ResolveAsync(() => applicationRepository.GetContractIdByIdAsync(applicationId));

    public Task<IContract> ResolveByConcertIdAsync(int concertId) =>
        ResolveAsync(() => concertRepository.GetContractIdByIdAsync(concertId));

    private async Task<IContract> ResolveAsync(Func<Task<int?>> resolveContractId)
    {
        if (contract is not null)
            return contract;

        var contractId = await resolveContractId()
            ?? throw new NotFoundException("Contract not found for this entity");

        return contract = await contractModule.GetByIdAsync(contractId)
            ?? throw new NotFoundException($"No contract with id {contractId}");
    }
}
