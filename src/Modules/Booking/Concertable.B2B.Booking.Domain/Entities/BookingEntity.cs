using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Concertable.Kernel;
using Reunion;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(DisplayNames.Booking)]
public abstract class BookingEntity : IIdEntity, IVenueArtistTenantScoped, IConcurrencyVersioned, IEventRaiser
{
    private static readonly BookingStateMachine stateMachine = new();

    public int Id { get; private set; }
    public byte[] Version { get; private set; } = null!;
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid OperationId { get; private set; }
    public int ApplicationId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public DealType DealType { get; private set; }
    public bool RequiresDoorRevenue { get; private set; }
    internal FinancialOperation ExpectedFinancialOperation { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public List<Genre> Genres { get; private set; } = [];
    internal BookingState State { get; private set; } = BookingState.AwaitingConfirmation;
    public Guid? CancellationOperationId { get; private set; }
    public string? FinancialFailureCode { get; private set; }
    public string? FinancialFailureMessage { get; private set; }
    public string? FinancialOperationReferenceId { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected BookingEntity() { }

    private protected BookingEntity(BookingAcceptance acceptance)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        if (acceptance.VenueTenantId == Guid.Empty || acceptance.ArtistTenantId == Guid.Empty)
            throw new InvalidOperationException("A booking cannot inherit unresolved application tenants.");

        OperationId = acceptance.OperationId;
        ApplicationId = acceptance.ApplicationId;
        OpportunityId = acceptance.OpportunityId;
        ArtistId = acceptance.ArtistId;
        VenueId = acceptance.VenueId;
        DealType = acceptance.DealType;
        RequiresDoorRevenue = acceptance.RequiresDoorRevenue;
        ExpectedFinancialOperation = acceptance switch
        {
            DeferredBookingAcceptance => FinancialOperation.VerifyPayment,
            StandardBookingAcceptance { DealType: DealType.FlatFee } => FinancialOperation.CaptureEscrow,
            StandardBookingAcceptance { DealType: DealType.VenueHire } => FinancialOperation.DepositEscrow,
            _ => throw new ArgumentOutOfRangeException(nameof(acceptance), acceptance, null)
        };
        StartDate = acceptance.StartDate;
        EndDate = acceptance.EndDate;
        Genres = acceptance.Genres.ToList();
        VenueTenantId = acceptance.VenueTenantId;
        ArtistTenantId = acceptance.ArtistTenantId;
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordFinancialConfirmation(string providerReferenceId)
    {
        var transition = Fire(BookingTrigger.Confirm);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        events.Raise(new BookingConfirmedDomainEvent(new ConfirmedBooking(
            OperationId,
            Id,
            ApplicationId,
            OpportunityId,
            ArtistId,
            VenueId,
            VenueTenantId,
            ArtistTenantId,
            DealType,
            RequiresDoorRevenue,
            StartDate,
            EndDate,
            Genres,
            GetConfirmedTerms())));
        return new Success();
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordFinancialFailure(
        string providerReferenceId,
        string code,
        string message)
    {
        var transition = Fire(BookingTrigger.RecordConfirmationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
        return new Success();
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordFinancialRejection(string code, string message)
    {
        var transition = Fire(BookingTrigger.RecordConfirmationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = null;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
        return new Success();
    }

    internal Result<Guid, TransitionError<BookingState, BookingTrigger>> BeginCancellation()
    {
        var transition = Fire(BookingTrigger.BeginCancellation);
        if (transition.TryGetError(out var error))
            return error;
        CancellationOperationId = Guid.NewGuid();
        return CancellationOperationId.Value;
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> ValidateBeginCancellation() =>
        Validate(BookingTrigger.BeginCancellation);

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordCancellationFailure(string code, string message)
    {
        var transition = Fire(BookingTrigger.RecordCancellationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
        return new Success();
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> Cancel()
    {
        var transition = Fire(BookingTrigger.Cancel);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        events.Raise(new BookingCancelledDomainEvent(Id, ApplicationId, OpportunityId));
        return new Success();
    }

    private UnitResult<TransitionError<BookingState, BookingTrigger>> Fire(BookingTrigger trigger)
    {
        var transition = Transition(trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private UnitResult<TransitionError<BookingState, BookingTrigger>> Validate(BookingTrigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private Result<BookingState, TransitionError<BookingState, BookingTrigger>> Transition(BookingTrigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        if (transition.TryGetValue(out var next))
            State = next;
        return transition;
    }

    protected abstract ConfirmedBookingTerms GetConfirmedTerms();
}

public sealed class StandardBooking : BookingEntity
{
    public decimal Amount { get; private set; }

    private StandardBooking() { }

    private StandardBooking(StandardBookingAcceptance acceptance) : base(acceptance)
    {
        Amount = acceptance.Amount;
    }

    internal static StandardBooking Create(StandardBookingAcceptance acceptance) => new(acceptance);

    protected override ConfirmedBookingTerms GetConfirmedTerms() => DealType switch
    {
        DealType.FlatFee => new FlatFeeBookingTerms(Amount),
        DealType.VenueHire => new VenueHireBookingTerms(Amount),
        _ => throw new InvalidOperationException($"Standard booking {Id} has unsupported deal type {DealType}.")
    };
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;
    public decimal ArtistDoorPercent { get; private set; }
    public decimal Guarantee { get; private set; }

    private DeferredBooking() { }

    private DeferredBooking(DeferredBookingAcceptance acceptance) : base(acceptance)
    {
        PaymentMethodId = acceptance.PaymentMethodId;
        ArtistDoorPercent = acceptance.ArtistDoorPercent;
        Guarantee = acceptance.Guarantee;
    }

    internal static DeferredBooking Create(DeferredBookingAcceptance acceptance) => new(acceptance);

    protected override ConfirmedBookingTerms GetConfirmedTerms() => DealType switch
    {
        DealType.DoorSplit => new DoorSplitBookingTerms(ArtistDoorPercent, PaymentMethodId),
        DealType.Versus => new VersusBookingTerms(Guarantee, ArtistDoorPercent, PaymentMethodId),
        _ => throw new InvalidOperationException($"Deferred booking {Id} has unsupported deal type {DealType}.")
    };
}
