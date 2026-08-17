using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(Booking.Contracts.DisplayNames.Booking)]
public abstract class BookingEntity : IIdEntity, IVenueArtistTenantScoped, IEventRaiser
{
    public int Id { get; private set; }
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
    internal BookingState State { get; private set; } = BookingState.AwaitingFinancialConfirmation;
    public Guid? CancellationOperationId { get; private set; }
    public string? FinancialFailureCode { get; private set; }
    public string? FinancialFailureMessage { get; private set; }
    public string? FinancialOperationReferenceId { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected BookingEntity() { }

    protected BookingEntity(AcceptedApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.VenueTenantId == Guid.Empty || application.ArtistTenantId == Guid.Empty)
            throw new InvalidOperationException("A booking cannot inherit unresolved application tenants.");

        OperationId = application.OperationId;
        ApplicationId = application.ApplicationId;
        OpportunityId = application.OpportunityId;
        ArtistId = application.ArtistId;
        VenueId = application.VenueId;
        DealType = application.DealType;
        RequiresDoorRevenue = application.RequiresDoorRevenue;
        ExpectedFinancialOperation = application switch
        {
            DoorSplitAcceptedApplication or VersusAcceptedApplication => FinancialOperation.VerifyPayment,
            FlatFeeAcceptedApplication => FinancialOperation.CaptureEscrow,
            VenueHireAcceptedApplication => FinancialOperation.DepositEscrow,
            _ => throw new ArgumentOutOfRangeException(nameof(application), application, null)
        };
        StartDate = application.StartDate;
        EndDate = application.EndDate;
        VenueTenantId = application.VenueTenantId;
        ArtistTenantId = application.ArtistTenantId;
    }

    internal void RecordFinancialConfirmation(string providerReferenceId)
    {
        if (State is not (BookingState.AwaitingFinancialConfirmation or BookingState.FinancialConfirmationFailed))
            throw new InvalidOperationException($"Booking {Id} cannot confirm from {State}.");

        State = BookingState.Confirmed;
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
            EndDate)));
    }

    internal void RecordFinancialFailure(
        string providerReferenceId,
        string code,
        string message)
    {
        if (State is not (BookingState.AwaitingFinancialConfirmation or BookingState.FinancialConfirmationFailed))
            throw new InvalidOperationException($"Booking {Id} cannot record payment failure from {State}.");

        State = BookingState.FinancialConfirmationFailed;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
    }

    internal Guid BeginCancellation()
    {
        if (State is not (BookingState.AwaitingFinancialConfirmation or BookingState.FinancialConfirmationFailed))
            throw new InvalidOperationException($"Booking {Id} cannot begin cancellation from {State}.");

        CancellationOperationId ??= Guid.NewGuid();
        State = BookingState.CancellationPending;
        return CancellationOperationId.Value;
    }

    internal void RecordCancellationFailure(string code, string message)
    {
        if (State != BookingState.CancellationPending)
            throw new InvalidOperationException($"Booking {Id} cannot record cancellation failure from {State}.");

        State = BookingState.CancellationFailed;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
    }

    internal void Cancel()
    {
        if (State is not (BookingState.CancellationPending or BookingState.CancellationFailed))
            throw new InvalidOperationException($"Booking {Id} cannot cancel from {State}.");

        State = BookingState.Cancelled;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
    }
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(AcceptedApplication application) : base(application) { }

    public static StandardBooking Create(AcceptedApplication application) => new(application);
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private DeferredBooking() { }

    private DeferredBooking(AcceptedApplication application, string paymentMethodId) : base(application) =>
        PaymentMethodId = paymentMethodId;

    public static DeferredBooking Create(AcceptedApplication application, string paymentMethodId) =>
        new(application, paymentMethodId);
}
