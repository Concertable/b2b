using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Factories;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.Booking.Infrastructure.Specifications;
using Concertable.B2B.Booking.Infrastructure.Strategies;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Deal.Contracts;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingWorkflow : IBookingWorkflow
{
    private readonly IBookingRepository bookingRepository;
    private readonly IBookingPrivilegedRepository privilegedRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly IPrivilegedUnitOfWorkBehavior privilegedUnitOfWorkBehavior;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior;
    private readonly IBus bus;
    private readonly IDealStrategyFactory<IConfirmStep> confirmFactory;
    private readonly IDealStrategyFactory<ICancelStep> cancelFactory;
    private readonly IDealStrategyFactory<IContractFactory> contractFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<BookingWorkflow> logger;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;

    public BookingWorkflow(
        IBookingRepository bookingRepository,
        IBookingPrivilegedRepository privilegedRepository,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        IPrivilegedUnitOfWorkBehavior privilegedUnitOfWorkBehavior,
        IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior,
        IBus bus,
        IDealStrategyFactory<IConfirmStep> confirmFactory,
        IDealStrategyFactory<ICancelStep> cancelFactory,
        IDealStrategyFactory<IContractFactory> contractFactory,
        TimeProvider timeProvider,
        ILogger<BookingWorkflow> logger,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor)
    {
        this.bookingRepository = bookingRepository;
        this.privilegedRepository = privilegedRepository;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.privilegedUnitOfWorkBehavior = privilegedUnitOfWorkBehavior;
        this.privilegedOutboxUnitOfWorkBehavior = privilegedOutboxUnitOfWorkBehavior;
        this.bus = bus;
        this.confirmFactory = confirmFactory;
        this.cancelFactory = cancelFactory;
        this.contractFactory = contractFactory;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
    }

    public Task<BookingDto> ConfirmAsync(
        AcceptedApplication application,
        CancellationToken ct = default) =>
        outboxUnitOfWorkBehavior.ExecuteAsync(() => ConfirmCoreAsync(application, ct), ct);

    public async Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new CancelBookingError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<BookingWorkflow, UnitResult<CancelBookingError>>(
                (workflow, token) => workflow.CancelCommandAsync(bookingId, actor, token),
                (workflow, _, token) => workflow.ValidateCancelAuthorityAsync(bookingId, actor, token),
                () => new CancelBookingError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsBookingConcurrencyConflict(bookingId))
        {
            return await commandExecutor.ExecuteAsync<BookingWorkflow, UnitResult<CancelBookingError>>(
                (workflow, token) => workflow.ClassifyCancelConflictAsync(bookingId, actor, token),
                ct);
        }
    }

    public Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default) =>
        privilegedUnitOfWorkBehavior.ExecuteAsync(
            () => RecordSucceededCoreAsync(bookingId, operation, ct),
            ct);

    public Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default) =>
        privilegedUnitOfWorkBehavior.ExecuteAsync(
            () => RecordFailedCoreAsync(bookingId, operation, ct),
            ct);

    private async Task<UnitResult<CancelBookingError>> ClassifyCancelConflictAsync(
        int bookingId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
        => await privilegedUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            if (!await ValidateCancelAuthorityAsync(bookingId, expectedActor, ct))
                return (UnitResult<CancelBookingError>)new CancelBookingError.NotPermitted();

            if (await privilegedRepository.GetStateByIdAsync(bookingId, ct)
                is BookingState.Cancelled or BookingState.CancellationPending)
                return (UnitResult<CancelBookingError>)new Success();

            return new CancelBookingError.Superseded(bookingId);
        }, ct);

    private Task<UnitResult<CancelBookingError>> CancelCommandAsync(
        int bookingId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => CancelCoreAsync(bookingId, actor, ct),
            ct);

    private async Task<UnitResult<CancelBookingError>> CancelCoreAsync(
        int bookingId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.BookingsCancel))
            return new CancelBookingError.NotPermitted();

        var booking = await privilegedRepository.GetByIdForUpdateAsync(bookingId, ct);
        if (booking is null)
            return new CancelBookingError.BookingNotFound(bookingId);
        if (!await CanCancelAsync(bookingId, actor, ct))
            return new CancelBookingError.NotPermitted();
        if (booking.State is BookingState.Cancelled or BookingState.CancellationPending)
            return new Success();
        if (booking.ValidateBeginCancellation().TryGetError(out var transitionError))
            return new CancelBookingError.InvalidTransition(transitionError);

        await cancelFactory.Create(booking.DealType).CancelAsync(booking, ct);
        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<bool> ValidateCancelAuthorityAsync(
        int bookingId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
            && permissionCatalog.Grants(actor.Role, TenantPermission.BookingsCancel)
            && await CanCancelAsync(bookingId, actor, ct);
    }

    private Task<bool> CanCancelAsync(
        int bookingId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedRepository.CanCancelAsync(
            bookingId,
            actor,
            permissionCatalog.AudienceFor(actor.Role, TenantPermission.BookingsCancel),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    private async Task<BookingDto> ConfirmCoreAsync(
        AcceptedApplication application,
        CancellationToken ct)
    {
        var snapshot = application.Snapshot;
        var booking = await CreateAsync(
            snapshot,
            (bookingId, createdAtUtc) => CreateContract(
                application,
                bookingId,
                createdAtUtc),
            ct);
        await confirmFactory.Create(booking.DealType).ConfirmAsync(booking, ct);
        await bookingRepository.SaveChangesAsync(ct);
        return booking.ToDto();
    }

    private ContractEntity CreateContract(
        AcceptedApplication application,
        int bookingId,
        DateTime createdAtUtc) =>
        contractFactory
            .Create(application.Snapshot.Contract.Terms.DealType)
            .Create(bookingId, application.Snapshot, createdAtUtc);

    private Task<BookingEntity> CreateAsync(
        ApplicationAcceptanceSnapshot snapshot,
        Func<int, DateTime, ContractEntity> mintContract,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(() => CreateCoreAsync(snapshot, mintContract, ct), ct);

    private async Task<BookingEntity> CreateCoreAsync(
        ApplicationAcceptanceSnapshot snapshot,
        Func<int, DateTime, ContractEntity> mintContract,
        CancellationToken ct)
    {
        var booking = BookingEntity.Create(snapshot);
        await bookingRepository.AddAsync(booking, ct);
        await bookingRepository.SaveChangesAsync(ct);

        var contract = mintContract(booking.Id, timeProvider.GetUtcNow().UtcDateTime);
        booking.MintContract(contract);
        await bookingRepository.AddContractAsync(contract, ct);
        await bookingRepository.SaveChangesAsync(ct);
        return booking;
    }

    private async Task RecordSucceededCoreAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct)
    {
        var booking = await privilegedRepository.GetWithContractByIdForUpdateAsync(bookingId, ct);
        if (booking is null || !Matches(bookingId, booking, operation))
        {
            logger.FinancialOutcomeSkipped(operation.Operation, bookingId);
            return;
        }

        if (booking.State == BookingState.CancellationPending)
        {
            await bus.SendAsync(new RefundEscrowCommand(
                booking.CancellationOperationId!.Value,
                PaymentOperationReferences.Escrow(bookingId),
                RefundReasonCodes.RequestedByPayer), ct);
            return;
        }
        if (booking.State is BookingState.CancellationFailed or BookingState.Cancelled)
            return;
        if (booking.State == BookingState.Confirmed)
            return;

        if (booking.RecordFinancialConfirmation().TryGetError(out var transitionError))
            throw new InvalidOperationException($"Booking cannot confirm from {transitionError.Current}.");
        await privilegedRepository.SaveChangesAsync(ct);
    }

    private async Task RecordFailedCoreAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct)
    {
        var booking = await privilegedRepository.GetByIdForUpdateAsync(bookingId, ct);
        if (booking is null || !Matches(bookingId, booking, operation))
        {
            logger.FinancialOutcomeSkipped(operation.Operation, bookingId);
            return;
        }

        if (booking.State == BookingState.Confirmed)
            return;
        if (booking.State is BookingState.CancellationFailed or BookingState.Cancelled)
            return;
        if (booking.State == BookingState.CancellationPending)
        {
            if (booking.Cancel().TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot cancel from {transitionError.Current}.");
            await privilegedRepository.SaveChangesAsync(ct);
            return;
        }
        if (IsDuplicateFailure(booking, operation))
            return;

        if (booking.RecordFinancialFailure(operation.Error.Code, operation.Error.Message)
            .TryGetError(out var failureError))
            throw new InvalidOperationException($"Booking cannot record confirmation failure from {failureError.Current}.");
        await privilegedRepository.SaveChangesAsync(ct);
    }

    private static bool Matches(
        int bookingId,
        BookingEntity booking,
        FinancialOperationEvidence operation) =>
        booking.ExpectedFinancialOperation == operation.Operation
        && operation switch
        {
            VerifyPaymentSucceededEvidence verified => booking.ApplicationId == verified.ApplicationId,
            VerifyPaymentFailedEvidence failed => booking.ApplicationId == failed.ApplicationId,
            AcceptanceFinancialOperationSucceeded accepted =>
                bookingId == accepted.BookingId && booking.OperationId == accepted.OperationId,
            AcceptanceFinancialOperationRejected rejected =>
                bookingId == rejected.BookingId && booking.OperationId == rejected.OperationId,
            _ => false
        };

    private static bool IsDuplicateFailure(
        BookingEntity booking,
        FinancialOperationFailed operation) =>
        booking.State == BookingState.ConfirmationFailed
        && booking.FinancialFailure?.Code == operation.Error.Code
        && booking.FinancialFailure?.Message == operation.Error.Message;
}
