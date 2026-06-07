using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

public abstract class ApplicationEntity : IIdEntity
{
    public int Id { get; private set; }
    internal LifecycleState State { get; private set; } = LifecycleState.Applied;
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public ContractType ContractType { get; private set; }
    public OpportunityEntity Opportunity { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public BookingEntity? Booking { get; set; }

    protected ApplicationEntity() { }

    protected ApplicationEntity(int artistId, int opportunityId, ContractType contractType)
    {
        ArtistId = artistId;
        OpportunityId = opportunityId;
        ContractType = contractType;
    }

    public void Accept(BookingEntity booking) => Booking = booking;

    internal void Transition(Trigger trigger, ContractStateMachine machine) => State = machine.Next(State, trigger);
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
