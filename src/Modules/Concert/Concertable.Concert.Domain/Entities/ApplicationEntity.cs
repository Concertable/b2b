using Concertable.Concert.Domain.Events;

namespace Concertable.Concert.Domain;

public abstract class ApplicationEntity : IIdEntity, ILifecycleEntity, IEventRaiser
{
    public int Id { get; private set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Pending;
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public ContractType ContractType { get; private set; }
    public ConcertStage CurrentStage { get; private set; } = ConcertStage.None;
    public OpportunityEntity Opportunity { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public BookingEntity? Booking { get; set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected ApplicationEntity() { }

    protected ApplicationEntity(int artistId, int opportunityId, ContractType contractType)
    {
        ArtistId = artistId;
        OpportunityId = opportunityId;
        ContractType = contractType;
    }

    public void AdvanceStage(ConcertStage next)
    {
        if (next is not (ConcertStage.Applied or ConcertStage.Verified or ConcertStage.Accepted))
            throw new DomainException($"ApplicationEntity cannot advance to {next}.");
        CurrentStage = next;
        if (next == ConcertStage.Accepted)
            events.Raise(new ApplicationAcceptedDomainEvent(Id, OpportunityId));
    }

    public void Accept(BookingEntity bookingConcert)
    {
        if (Status != ApplicationStatus.Pending)
            throw new DomainException("Only pending applications can be accepted.");
        Status = ApplicationStatus.Accepted;
        Booking = bookingConcert;
    }

    public void Reject()
    {
        if (Status != ApplicationStatus.Pending)
            throw new DomainException("Only pending applications can be rejected.");
        Status = ApplicationStatus.Rejected;
    }

    public void Withdraw()
    {
        if (Status != ApplicationStatus.Pending)
            throw new DomainException("Only pending applications can be withdrawn.");
        Status = ApplicationStatus.Withdrawn;
    }
}

public sealed class StandardApplication : ApplicationEntity
{
    private StandardApplication() { }

    private StandardApplication(int artistId, int opportunityId, ContractType contractType)
        : base(artistId, opportunityId, contractType) { }

    public static StandardApplication Create(int artistId, int opportunityId) =>
        new(artistId, opportunityId, default);

    public static StandardApplication Create(int artistId, int opportunityId, ContractType contractType) =>
        new(artistId, opportunityId, contractType);
}

public sealed class PrepaidApplication : ApplicationEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private PrepaidApplication() { }

    private PrepaidApplication(int artistId, int opportunityId, ContractType contractType, string paymentMethodId)
        : base(artistId, opportunityId, contractType)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static PrepaidApplication Create(int artistId, int opportunityId, string paymentMethodId) =>
        new(artistId, opportunityId, default, paymentMethodId);

    public static PrepaidApplication Create(int artistId, int opportunityId, ContractType contractType, string paymentMethodId) =>
        new(artistId, opportunityId, contractType, paymentMethodId);
}
