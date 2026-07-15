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
    private readonly IDealResolver dealResolver;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly TimeProvider timeProvider;

    public ApplyExecutor(
        IApplicationRepository applicationRepository,
        IOpportunityRepository opportunityRepository,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IClientContext clientContext,
        ITermsFingerprintCalculator termsFingerprint,
        TimeProvider timeProvider)
    {
        this.applicationRepository = applicationRepository;
        this.opportunityRepository = opportunityRepository;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.termsFingerprint = termsFingerprint;
        this.timeProvider = timeProvider;
    }

    public async Task<ApplicationEntity> ExecuteAsync(int opportunityId, int artistId, string? paymentMethodId, ESignatureRequest eSignature)
    {
        var deal = await dealResolver.ResolveByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(deal.DealType);
        var application = workflow switch
        {
            IAppliesPaid w when paymentMethodId is not null
                => await w.Apply.ApplyAsync(artistId, opportunityId, deal.DealType, paymentMethodId),
            IAppliesSimple w
                => await w.Apply.ApplyAsync(artistId, opportunityId, deal.DealType),
            _ => throw new BadRequestException($"Deal {workflow.Type} does not support Apply")
        };

        /* Snapshot the two parties at apply; the booking and concert inherit this pair downstream.
           The applier IS the artist side, so their own tenant comes from the ambient context. */
        application.VenueTenantId = await opportunityRepository.GetTenantIdByIdAsync(opportunityId)
            .OrNotFound(DisplayNames.Opportunity);
        application.ArtistTenantId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for current user");

        var period = await opportunityRepository.GetPeriodByIdAsync(opportunityId)
            .OrNotFound(DisplayNames.Opportunity);
        application.RecordArtistESignature(
            new ESignature(
                currentUser.Id ?? throw new ForbiddenException("No user for current request"),
                timeProvider.GetUtcNow().UtcDateTime,
                clientContext.IpAddress,
                clientContext.UserAgent,
                eSignature.SignatoryName,
                eSignature.DrawnSignatureImage),
            termsFingerprint.Calculate(deal, period));

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
