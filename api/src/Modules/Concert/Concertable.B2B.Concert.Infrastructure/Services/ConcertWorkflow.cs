using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertWorkflow : IConcertWorkflow
{
    private readonly IConcertPrivilegedRepository privilegedRepository;
    private readonly ICommandExecutor commandExecutor;
    private readonly IDealStrategyFactory<ICancelStep> cancelFactory;
    private readonly IDealStrategyFactory<ICompleteStep> completeFactory;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly TimeProvider timeProvider;

    public ConcertWorkflow(
        IConcertPrivilegedRepository privilegedRepository,
        ICommandExecutor commandExecutor,
        IDealStrategyFactory<ICancelStep> cancelFactory,
        IDealStrategyFactory<ICompleteStep> completeFactory,
        IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        TimeProvider timeProvider)
    {
        this.privilegedRepository = privilegedRepository;
        this.commandExecutor = commandExecutor;
        this.cancelFactory = cancelFactory;
        this.completeFactory = completeFactory;
        this.privilegedOutboxUnitOfWorkBehavior = privilegedOutboxUnitOfWorkBehavior;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.timeProvider = timeProvider;
    }

    public async Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new CancelConcertError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertWorkflow, UnitResult<CancelConcertError>>(
                (workflow, token) => workflow.CancelCommandAsync(concertId, actor, token),
                (workflow, _, token) => workflow.ValidateCancelAuthorityAsync(concertId, actor, token),
                () => new CancelConcertError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(concertId))
        {
            return await commandExecutor.ExecuteAsync<ConcertWorkflow, UnitResult<CancelConcertError>>(
                (workflow, token) => workflow.ClassifyCancelConflictAsync(concertId, actor, token),
                ct);
        }
    }

    public async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default)
    {
        var prepared = await commandExecutor.ExecuteAsync<ISettlementService, Result<SettlementPreparation, FinishConcertError>>(
            (service, token) => service.ReserveAsync(concertId, token),
            ct);
        if (prepared.TryGetError(out var error))
            return error;
        if (!prepared.TryGetValue(out var preparation))
            throw new InvalidOperationException($"Concert {concertId} settlement preparation returned no value.");

        if (preparation is SettlementPreparation.Terminal terminal)
            return terminal.Outcome;
        if (preparation is not SettlementPreparation.Ready ready)
            throw new InvalidOperationException(
                $"Concert {concertId} returned an unknown settlement preparation.");

        var executed = await completeFactory.Create(ready.DealType).CompleteAsync(ready, ct);
        if (executed.TryGetError(out var executionError))
            return executionError;

        return await commandExecutor.ExecuteAsync<ISettlementService, Result<SettlementOutcome, FinishConcertError>>(
            (service, token) => service.CompleteAsync(ready.ConcertId, ready.OperationId, token),
            ct);
    }

    private async Task<UnitResult<CancelConcertError>> ClassifyCancelConflictAsync(
        int concertId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
        => await privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            if (!await ValidateCancelAuthorityAsync(concertId, expectedActor, ct))
                return (UnitResult<CancelConcertError>)new CancelConcertError.NotPermitted();

            if (await privilegedRepository.GetStateByIdAsync(concertId, ct)
                is ConcertState.Cancelled or ConcertState.CancellationPending)
                return (UnitResult<CancelConcertError>)new Success();

            return new CancelConcertError.Superseded(concertId);
        }, ct);

    private Task<UnitResult<CancelConcertError>> CancelCommandAsync(
        int concertId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => CancelCoreAsync(concertId, actor, ct),
            ct);

    private async Task<UnitResult<CancelConcertError>> CancelCoreAsync(
        int concertId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsManage))
            return new CancelConcertError.NotPermitted();

        var concert = await privilegedRepository.GetByIdForUpdateAsync(concertId, ct);
        if (concert is null)
            return new CancelConcertError.ConcertNotFound(concertId);
        if (!await CanCancelAsync(concertId, actor, ct))
            return new CancelConcertError.NotPermitted();
        if (concert.State is ConcertState.Cancelled or ConcertState.CancellationPending)
            return new Success();
        if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
            return new CancelConcertError.InvalidTransition(transitionError);

        await cancelFactory.Create(concert.DealType).CancelAsync(concert, ct);
        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<bool> ValidateCancelAuthorityAsync(
        int concertId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
            && permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsManage)
            && await CanCancelAsync(concertId, actor, ct);
    }

    private Task<bool> CanCancelAsync(
        int concertId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedRepository.CanManageAsync(
            concertId,
            actor,
            permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsManage),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);
}
