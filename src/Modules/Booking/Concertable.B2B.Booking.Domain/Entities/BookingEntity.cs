using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Concertable.Kernel;
using Reunion;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(Booking.Contracts.DisplayNames.Booking)]
public sealed class BookingEntity : IIdEntity, IVenueArtistTenantScoped, IConcurrencyVersioned, IEventRaiser
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
    internal FinancialOperation ExpectedFinancialOperation { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public List<Genre> Genres { get; private set; } = [];
    internal BookingState State { get; private set; } = BookingState.AwaitingConfirmation;
    public Guid? CancellationOperationId { get; private set; }
    internal FinancialFailure? FinancialFailure { get; private set; }
    public string? FinancialOperationReferenceId { get; private set; }
    public ContractEntity Contract { get; private set; } = null!;

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    private BookingEntity() { }

    internal static BookingEntity Create(ApplicationAcceptanceSnapshot snapshot) => new(snapshot);

    internal void MintContract(ContractEntity contract)
    {
        Contract = contract;
        ExpectedFinancialOperation = contract.ExpectedFinancialOperation;
    }

    private BookingEntity(ApplicationAcceptanceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var application = snapshot.Application;
        var opportunity = application.Opportunity;
        if (opportunity.Venue.TenantId == Guid.Empty || application.Artist.TenantId == Guid.Empty)
            throw new InvalidOperationException("A booking cannot inherit unresolved application tenants.");

        OperationId = snapshot.OperationId;
        ApplicationId = application.Id;
        OpportunityId = opportunity.Id;
        ArtistId = application.Artist.Id;
        VenueId = opportunity.Venue.Id;
        DealType = snapshot.Contract.Terms.DealType;
        StartDate = opportunity.StartDate;
        EndDate = opportunity.EndDate;
        Genres = opportunity.Genres.ToList();
        VenueTenantId = opportunity.Venue.TenantId;
        ArtistTenantId = application.Artist.TenantId;
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordFinancialConfirmation(
        string providerReferenceId)
    {
        var transition = Fire(BookingTrigger.Confirm);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailure = null;
        events.Raise(new BookingConfirmedDomainEvent(new ConfirmedBookingSnapshot(
            Id,
            ApplicationId,
            OpportunityId,
            ArtistId,
            VenueId,
            VenueTenantId,
            ArtistTenantId,
            StartDate,
            EndDate,
            Genres,
            Contract.Commitment,
            Contract.ConfirmedTerms)));
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
        FinancialFailure = new FinancialFailure(code, message);
        return new Success();
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> RecordFinancialRejection(string code, string message)
    {
        var transition = Fire(BookingTrigger.RecordConfirmationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = null;
        FinancialFailure = new FinancialFailure(code, message);
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
        FinancialFailure = new FinancialFailure(code, message);
        return new Success();
    }

    internal UnitResult<TransitionError<BookingState, BookingTrigger>> Cancel()
    {
        var transition = Fire(BookingTrigger.Cancel);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailure = null;
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
}
