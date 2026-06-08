using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class ApplyExecutor : IApplyExecutor
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;

    public ApplyExecutor(
        IApplicationRepository applicationRepository,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver)
    {
        this.applicationRepository = applicationRepository;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
    }

    public async Task<ApplicationEntity> ExecuteAsync(int opportunityId, int artistId, string? paymentMethodId)
    {
        var contract = await contractResolver.ResolveByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(contract.ContractType);
        var application = workflow switch
        {
            IAppliesPaid w when paymentMethodId is not null
                => await w.Apply.ApplyAsync(artistId, opportunityId, contract.ContractType, paymentMethodId),
            IAppliesSimple w
                => await w.Apply.ApplyAsync(artistId, opportunityId, contract.ContractType),
            _ => throw new BadRequestException($"Contract {workflow.Type} does not support Apply")
        };

        await applicationRepository.AddAsync(application);
        try
        {
            await applicationRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            throw new BadRequestException("You cannot apply to the same concert opportunity twice");
        }
        return application;
    }
}
