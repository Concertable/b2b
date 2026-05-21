using Concertable.Concert.Domain.Events;

namespace Concertable.Concert.Domain;

public abstract class BookingEntity : IIdEntity, ILifecycleEntity, IEventRaiser
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public BookingStatus Status { get; private set; }
    public ContractType ContractType { get; private set; }
    public ConcertStage CurrentStage { get; private set; } = ConcertStage.Accepted;
    public ApplicationEntity Application { get; set; } = null!;
    public ConcertEntity? Concert { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected BookingEntity() { }

    protected BookingEntity(int applicationId, ContractType contractType)
    {
        ApplicationId = applicationId;
        ContractType = contractType;
        Status = BookingStatus.Pending;
    }

    public void AdvanceStage(ConcertStage next)
    {
        if (next is not (ConcertStage.Accepted or ConcertStage.Settled))
            throw new DomainException($"BookingEntity cannot advance to {next}.");
        CurrentStage = next;
        if (next == ConcertStage.Settled)
            events.Raise(new BookingSettledDomainEvent(Id, ContractType));
    }

    public void AwaitPayment()
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed)
            throw new DomainException("Only pending or confirmed bookings can await payment.");
        Status = BookingStatus.AwaitingPayment;
    }

    public void Confirm(ConcertEntity concert)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.AwaitingPayment)
            throw new DomainException("Only pending or awaiting payment bookings can be confirmed.");
        Concert = concert;
        Status = BookingStatus.Confirmed;
    }

    public void Complete()
    {
        if (Status != BookingStatus.AwaitingPayment && Status != BookingStatus.Confirmed)
            throw new DomainException("Only awaiting payment or confirmed bookings can be completed.");
        if (DateTime.UtcNow < Application.Opportunity.Period.End)
            throw new DomainException("Booking cannot be completed before the concert has ended.");
        Status = BookingStatus.Complete;
    }

    public void FailPayment()
    {
        if (Status != BookingStatus.AwaitingPayment)
            throw new DomainException("Only bookings awaiting payment can fail.");
        Status = BookingStatus.PaymentFailed;
    }
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(int applicationId, ContractType contractType)
        : base(applicationId, contractType) { }

    public static StandardBooking Create(int applicationId) =>
        new(applicationId, default);

    public static StandardBooking Create(int applicationId, ContractType contractType) =>
        new(applicationId, contractType);
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private DeferredBooking() { }

    private DeferredBooking(int applicationId, ContractType contractType, string paymentMethodId)
        : base(applicationId, contractType)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static DeferredBooking Create(int applicationId, string paymentMethodId) =>
        new(applicationId, default, paymentMethodId);

    public static DeferredBooking Create(int applicationId, ContractType contractType, string paymentMethodId) =>
        new(applicationId, contractType, paymentMethodId);
}
