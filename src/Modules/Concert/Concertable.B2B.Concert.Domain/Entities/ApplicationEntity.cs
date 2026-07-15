using System.ComponentModel;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

[DisplayName(DisplayNames.Application)]
public abstract class ApplicationEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    internal LifecycleState State { get; private set; } = LifecycleState.Applied;
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public DealType DealType { get; private set; }
    public OpportunityEntity Opportunity { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public BookingEntity? Booking { get; set; }

    public ESignature ArtistESignature { get; private set; } = null!;
    public string TermsFingerprint { get; private set; } = null!;

    protected ApplicationEntity() { }

    protected ApplicationEntity(int artistId, int opportunityId, DealType dealType)
    {
        ArtistId = artistId;
        OpportunityId = opportunityId;
        DealType = dealType;
    }

    public void Accept(BookingEntity booking) => Booking = booking;

    public void RecordArtistESignature(ESignature eSignature, string termsFingerprint)
    {
        ArtistESignature = eSignature;
        TermsFingerprint = termsFingerprint;
    }

    internal void Transition(Trigger trigger, LifecycleStateMachine machine) => State = machine.Next(State, trigger);
}

public sealed class StandardApplication : ApplicationEntity
{
    private StandardApplication() { }

    private StandardApplication(int artistId, int opportunityId, DealType dealType)
        : base(artistId, opportunityId, dealType) { }

    public static StandardApplication Create(int artistId, int opportunityId) =>
        new(artistId, opportunityId, default);

    public static StandardApplication Create(int artistId, int opportunityId, DealType dealType) =>
        new(artistId, opportunityId, dealType);
}

public sealed class PrepaidApplication : ApplicationEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private PrepaidApplication() { }

    private PrepaidApplication(int artistId, int opportunityId, DealType dealType, string paymentMethodId)
        : base(artistId, opportunityId, dealType)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static PrepaidApplication Create(int artistId, int opportunityId, string paymentMethodId) =>
        new(artistId, opportunityId, default, paymentMethodId);

    public static PrepaidApplication Create(int artistId, int opportunityId, DealType dealType, string paymentMethodId) =>
        new(artistId, opportunityId, dealType, paymentMethodId);
}
