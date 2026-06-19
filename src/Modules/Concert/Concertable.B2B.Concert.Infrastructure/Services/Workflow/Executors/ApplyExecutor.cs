using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class ApplyExecutor : IApplyExecutor
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IContractResolver contractResolver;
    private readonly ITenantContext tenantContext;

    public ApplyExecutor(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver,
        ITenantContext tenantContext)
    {
        this.applicationRepository = applicationRepository;
        this.opportunityRepository = opportunityRepository;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
        this.tenantContext = tenantContext;
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

        /* Snapshot the two parties at apply; the booking and concert inherit this pair downstream.
           The applier IS the artist side, so their own tenant comes from the ambient context. */
        application.VenueTenantId = await opportunityRepository.GetTenantIdByIdAsync(opportunityId)
            ?? throw new NotFoundException("Concert Opportunity not found");
        application.ArtistTenantId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for current user");

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
