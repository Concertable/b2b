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
    private readonly ITransactionRunner transactionRunner;
    private readonly IDealStrategyFactory<ICancelStep> cancelFactory;
    private readonly IDealStrategyFactory<ICompleteStep> completeFactory;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior;
    private readonly IMembershipContext membership;
    private readonly IResourceAuthorization resources;
    private readonly IAuthorizationContext authorizationContext;

    public ConcertWorkflow(
        IConcertPrivilegedRepository privilegedRepository,
        ITransactionRunner transactionRunner,
        IDealStrategyFactory<ICancelStep> cancelFactory,
        IDealStrategyFactory<ICompleteStep> completeFactory,
        IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior,
        IMembershipContext membership,
        IResourceAuthorization resources,
        IAuthorizationContext authorizationContext)
    {
        this.privilegedRepository = privilegedRepository;
        this.transactionRunner = transactionRunner;
        this.cancelFactory = cancelFactory;
        this.completeFactory = completeFactory;
        this.privilegedOutboxUnitOfWorkBehavior = privilegedOutboxUnitOfWorkBehavior;
        this.membership = membership;
        this.resources = resources;
        this.authorizationContext = authorizationContext;
    }

    public async Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
        {
            if (authorizationContext.IsActive)
            {
                authorizationContext.RegisterFailure<UnitResult<CancelConcertError>>(
                    () => new CancelConcertError.NotPermitted());
                authorizationContext.MarkAuthorityFailed();
            }
            return new CancelConcertError.NotPermitted();
        }

        try
        {
            return await transactionRunner.RunAsync<ConcertWorkflow, UnitResult<CancelConcertError>>(
                (workflow, token) => workflow.CancelCommandAsync(concertId, actor, token),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(concertId))
        {
            return await transactionRunner.RunAsync<ConcertWorkflow, UnitResult<CancelConcertError>>(
                (workflow, token) => workflow.ClassifyCancelConflictAsync(concertId, actor, token),
                ct);
        }
    }

    public async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default)
    {
        var prepared = await transactionRunner.RunAsync<ISettlementService, Result<SettlementPreparation, FinishConcertError>>(
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

        return await transactionRunner.RunAsync<ISettlementService, Result<SettlementOutcome, FinishConcertError>>(
            (service, token) => service.CompleteAsync(ready.ConcertId, ready.OperationId, token),
            ct);
    }

    private async Task<UnitResult<CancelConcertError>> ClassifyCancelConflictAsync(
        int concertId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
        => await privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            if (!await RequireCancellationAsync(concertId, expectedActor, ct))
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
        if (!await RequireCancellationAsync(concertId, expectedActor, ct))
            return new CancelConcertError.NotPermitted();

        var concert = await privilegedRepository.GetByIdForUpdateAsync(concertId, ct);
        if (concert is null)
            return new CancelConcertError.ConcertNotFound(concertId);
        if (concert.State is ConcertState.Cancelled or ConcertState.CancellationPending)
            return new Success();
        if (concert.ValidateBeginCancellation().TryGetError(out var transitionError))
            return new CancelConcertError.InvalidTransition(transitionError);

        await cancelFactory.Create(concert.DealType).CancelAsync(concert, ct);
        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<bool> RequireCancellationAsync(
        int concertId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<CancelConcertError>>(
            () => new CancelConcertError.NotPermitted());
        if (concertId <= 0 || membership.Membership is not { } actor
            || actor.MembershipId != expectedActor.MembershipId
            || actor.TenantId != expectedActor.TenantId)
        {
            authorizationContext.MarkAuthorityFailed();
            return false;
        }

        return await resources.RequireAsync(new AuthorizationRequest(
            TenantPermission.ConcertsManage,
            ResourceAddress.Create(ResourceKind.Concert, concertId),
            ResourceFacet.Operations), ct) == AuthorizationDecision.Allowed;
    }
}
