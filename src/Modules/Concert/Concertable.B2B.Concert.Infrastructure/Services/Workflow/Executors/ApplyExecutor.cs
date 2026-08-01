using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Infrastructure.Extensions;
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

    public async Task<Result<ApplicationEntity, ApplyApplicationError>> ApplyAsync(
        int opportunityId,
        int artistId,
        string? paymentMethodId,
        ESignatureRequest eSignature)
    {
        var deal = await dealResolver.ResolveByOpportunityIdAsync(opportunityId);
        var workflow = workflows.Create(deal.DealType);
        ApplicationEntity application;
        if (workflow is IAppliesPaid paid && paymentMethodId is not null)
        {
            application = await paid.Apply.ApplyAsync(artistId, opportunityId, deal.DealType, paymentMethodId);
        }
        else if (workflow is IAppliesSimple simple)
        {
            application = await simple.Apply.ApplyAsync(artistId, opportunityId, deal.DealType);
        }
        else
        {
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(
                ApplyApplicationError.UnsupportedDeal(workflow.Type));
        }

        /* Snapshot the two parties at apply; the booking and concert inherit this pair downstream.
           The applier IS the artist side, so their own tenant comes from the ambient context. */
        var venueTenantId = await opportunityRepository.GetTenantIdByIdAsync(opportunityId);
        if (venueTenantId is null)
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(
                ApplyApplicationError.OpportunityNotFound(opportunityId));
        application.VenueTenantId = venueTenantId.Value;

        if (tenantContext.TenantId is not { } artistTenantId)
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(ApplyApplicationError.MissingTenant());
        application.ArtistTenantId = artistTenantId;

        var period = await opportunityRepository.GetPeriodByIdAsync(opportunityId);
        if (period is null)
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(
                ApplyApplicationError.OpportunityNotFound(opportunityId));

        if (currentUser.Id is not { } userId)
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(ApplyApplicationError.MissingUser());

        application.RecordArtistESignature(
            new ESignature(
                userId,
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
            return Result.Failure<ApplicationEntity, ApplyApplicationError>(ApplyApplicationError.AlreadyApplied());
        }
        return Result.Success<ApplicationEntity, ApplyApplicationError>(application);
    }
}
