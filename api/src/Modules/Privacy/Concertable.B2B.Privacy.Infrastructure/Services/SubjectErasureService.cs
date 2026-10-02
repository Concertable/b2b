using Concertable.B2B.Privacy.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class SubjectErasureService : ISubjectErasureService
{
    private const string PendingFinancialObligations = "PendingFinancialObligations";

    private readonly ISubjectErasureRepository repository;
    private readonly ISubjectObligationChecker obligationChecker;
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;
    private readonly IConversationsModule conversationsModule;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SubjectErasureService> logger;

    public SubjectErasureService(
        ISubjectErasureRepository repository,
        ISubjectObligationChecker obligationChecker,
        IUserModule userModule,
        ITenantModule tenantModule,
        IConversationsModule conversationsModule,
        TimeProvider timeProvider,
        ILogger<SubjectErasureService> logger)
    {
        this.repository = repository;
        this.obligationChecker = obligationChecker;
        this.userModule = userModule;
        this.tenantModule = tenantModule;
        this.conversationsModule = conversationsModule;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<SubjectErasureRequestDto, ErasureTransitionError>> RequestErasureAsync(Guid subjectId, CancellationToken ct = default)
    {
        var request = await this.repository.GetBySubjectIdAsync(subjectId, ct);
        if (request is null)
        {
            request = SubjectErasureRequestEntity.Create(subjectId, this.timeProvider.GetUtcNow().UtcDateTime);
            await this.repository.InsertAsync(request, ct);
        }

        if (request.State == ErasureState.Completed)
            return request.ToDto();

        return await DriveAsync(request, ct);
    }

    private async Task<Result<SubjectErasureRequestDto, ErasureTransitionError>> DriveAsync(
        SubjectErasureRequestEntity request,
        CancellationToken ct)
    {
        if (await this.obligationChecker.HasLiveObligationsAsync(request.SubjectId, request.CapturedTenantIds, ct))
        {
            if (request.Fire(ErasureTrigger.Defer).TryGetError(out var deferError))
                return deferError;

            request.RecordDeferral(PendingFinancialObligations);
            await this.repository.SaveChangesAsync(ct);
            this.logger.SubjectErasureDeferred(request.SubjectId, request.Id);
            return request.ToDto();
        }

        if (request.Fire(ErasureTrigger.Begin).TryGetError(out var beginError))
            return beginError;

        await this.repository.SaveChangesAsync(ct);

        try
        {
            await AnonymiseAsync(request, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (request.Fire(ErasureTrigger.Fail).TryGetError(out _))
                throw;

            request.RecordFailure(exception.Message);
            await this.repository.SaveChangesAsync(ct);
            this.logger.DeferredErasureFailed(exception, request.SubjectId, request.Id);
            throw;
        }

        if (request.Fire(ErasureTrigger.Complete).TryGetError(out var completeError))
            return completeError;

        request.RecordCompletion(this.timeProvider.GetUtcNow().UtcDateTime);
        await this.repository.SaveChangesAsync(ct);
        this.logger.SubjectErasureCompleted(request.SubjectId, request.Id);
        return request.ToDto();
    }

    private async Task AnonymiseAsync(SubjectErasureRequestEntity request, CancellationToken ct)
    {
        var subjectId = request.SubjectId;
        if (request.TenantIds is null)
        {
            var user = await this.userModule.GetByIdAsync(subjectId);
            var memberships = await this.tenantModule.GetMembershipsAsync(subjectId, ct);
            request.CaptureFanOutState(user.Match<string?>(u => u.Email, () => null), memberships.Select(m => m.TenantId).ToHashSet());
            await this.repository.SaveChangesAsync(ct);
        }

        var woundDown = await this.tenantModule.SeverMembershipsAsync(subjectId, request.CapturedTenantIds, ct);
        request.RecordWoundDownTenants(woundDown);
        await this.repository.SaveChangesAsync(ct);

        if (request.SubjectEmail is not null)
            await this.tenantModule.PurgePendingInvitationsAsync(request.SubjectEmail, ct);

        await this.conversationsModule.SeverAuthoredMessagesAsync(subjectId, ct);
        await this.conversationsModule.ScrubParticipantProfilesAsync(request.CapturedTenantIds, ct);

        await this.userModule.EraseAsync(subjectId, ct);
    }
}
