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
        var request = await repository.GetBySubjectIdAsync(subjectId, ct);
        if (request is null)
        {
            request = SubjectErasureRequestEntity.Create(subjectId, timeProvider.GetUtcNow().UtcDateTime);
            await repository.InsertAsync(request, ct);
        }

        // Erasure is irreversible, so a completed request is the terminal answer to every later DSAR for the
        // same subject: hand it back untouched rather than re-running the fan-out over an already-scrubbed row.
        if (request.State == ErasureState.Completed)
            return request.ToDto();

        return await DriveAsync(request, ct);
    }

    private async Task<Result<SubjectErasureRequestDto, ErasureTransitionError>> DriveAsync(
        SubjectErasureRequestEntity request,
        CancellationToken ct)
    {
        if (await obligationChecker.HasLiveObligationsAsync(request.SubjectId, ct))
        {
            if (request.Fire(ErasureTrigger.Defer).TryGetError(out var deferError))
                return deferError;

            request.RecordDeferral(PendingFinancialObligations);
            await repository.SaveChangesAsync(ct);
            logger.SubjectErasureDeferred(request.SubjectId, request.Id);
            return request.ToDto();
        }

        if (request.Fire(ErasureTrigger.Begin).TryGetError(out var beginError))
            return beginError;

        await repository.SaveChangesAsync(ct);

        try
        {
            await AnonymiseAsync(request, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (request.Fire(ErasureTrigger.Fail).TryGetError(out _))
                throw;

            request.RecordFailure(exception.Message);
            await repository.SaveChangesAsync(ct);
            logger.DeferredErasureFailed(exception, request.SubjectId, request.Id);
            throw;
        }

        if (request.Fire(ErasureTrigger.Complete).TryGetError(out var completeError))
            return completeError;

        request.RecordCompletion(timeProvider.GetUtcNow().UtcDateTime);
        await repository.SaveChangesAsync(ct);
        logger.SubjectErasureCompleted(request.SubjectId, request.Id);
        return request.ToDto();
    }

    private async Task AnonymiseAsync(SubjectErasureRequestEntity request, CancellationToken ct)
    {
        var subjectId = request.SubjectId;
        if (request.SubjectEmail is null && request.WoundDownTenantIds is null)
        {
            var user = await userModule.GetByIdAsync(subjectId);
            var woundDown = await tenantModule.SeverMembershipsAsync(subjectId, ct);
            request.CaptureFanOutState(user.Match<string?>(u => u.Email, () => null), woundDown);
            await repository.SaveChangesAsync(ct);
        }

        if (request.SubjectEmail is not null)
            await tenantModule.PurgePendingInvitationsAsync(request.SubjectEmail, ct);

        await conversationsModule.SeverAuthoredMessagesAsync(subjectId, ct);
        await conversationsModule.ScrubParticipantProfilesAsync(request.CapturedWoundDownTenantIds, ct);

        await userModule.EraseAsync(subjectId, ct);
    }
}
