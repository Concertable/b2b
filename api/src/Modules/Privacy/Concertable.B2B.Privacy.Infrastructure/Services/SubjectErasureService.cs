using Concertable.B2B.Privacy.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class SubjectErasureService : ISubjectErasureService
{
    private const string PendingFinancialObligations = "PendingFinancialObligations";

    private readonly ISubjectErasureRepository repository;
    private readonly ISubjectObligationChecker obligationChecker;
    private readonly ErasureStateMachine stateMachine;
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;
    private readonly IConversationsModule conversationsModule;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SubjectErasureService> logger;

    public SubjectErasureService(
        ISubjectErasureRepository repository,
        ISubjectObligationChecker obligationChecker,
        ErasureStateMachine stateMachine,
        IUserModule userModule,
        ITenantModule tenantModule,
        IConversationsModule conversationsModule,
        TimeProvider timeProvider,
        ILogger<SubjectErasureService> logger)
    {
        this.repository = repository;
        this.obligationChecker = obligationChecker;
        this.stateMachine = stateMachine;
        this.userModule = userModule;
        this.tenantModule = tenantModule;
        this.conversationsModule = conversationsModule;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<SubjectErasureRequestDto> RequestErasureAsync(Guid subjectId, CancellationToken ct = default)
    {
        var request = SubjectErasureRequestEntity.Create(subjectId, timeProvider.GetUtcNow().UtcDateTime);
        await repository.InsertAsync(request, ct);

        if (await obligationChecker.HasLiveObligationsAsync(subjectId, ct))
        {
            Advance(request, ErasureTrigger.Defer);
            request.RecordDeferral(PendingFinancialObligations);
            await repository.SaveChangesAsync(ct);
            logger.SubjectErasureDeferred(subjectId, request.Id);
            return request.ToDto();
        }

        Advance(request, ErasureTrigger.Begin);
        await repository.SaveChangesAsync(ct);

        await AnonymiseAsync(subjectId, ct);

        Advance(request, ErasureTrigger.Complete);
        request.RecordCompletion(timeProvider.GetUtcNow().UtcDateTime);
        await repository.SaveChangesAsync(ct);
        logger.SubjectErasureCompleted(subjectId, request.Id);
        return request.ToDto();
    }

    // Resolve the subject's email BEFORE the User row is anonymised (which tombstones it), so pending
    // invitations addressed to them can still be matched and purged.
    private async Task AnonymiseAsync(Guid subjectId, CancellationToken ct)
    {
        var user = await userModule.GetByIdAsync(subjectId);
        var email = user.Match<string?>(u => u.Email, () => null);

        var woundDownTenantIds = await tenantModule.SeverMembershipsAsync(subjectId, ct);
        if (email is not null)
            await tenantModule.PurgePendingInvitationsAsync(email, ct);

        await conversationsModule.SeverAuthoredMessagesAsync(subjectId, ct);
        await conversationsModule.ScrubParticipantProfilesAsync(woundDownTenantIds, ct);

        await userModule.EraseAsync(subjectId, ct);
    }

    private void Advance(SubjectErasureRequestEntity request, ErasureTrigger trigger)
    {
        var transition = stateMachine.Next(request.State, trigger);
        if (transition.TryGetError(out var error))
            throw new InvalidOperationException(error.Definition.Message);

        transition.TryGetValue(out var next);
        request.Transition(next);
    }
}
