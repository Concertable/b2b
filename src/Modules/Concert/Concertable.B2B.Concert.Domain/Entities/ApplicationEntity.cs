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
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    internal LifecycleState State { get; private set; } = LifecycleState.Applied;
    internal PaymentVerification PaymentVerification { get; private set; } = PaymentVerification.None;
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public DealType DealType { get; private set; }
    public OpportunityEntity Opportunity { get; set; } = null!;
    public ArtistReadModel Artist { get; set; } = null!;
    public BookingEntity? Booking { get; private set; }

    public ESignature ArtistESignature { get; private set; } = null!;
    public string TermsFingerprint { get; private set; } = null!;

    protected ApplicationEntity() { }

    protected ApplicationEntity(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId)
    {
        if (venueTenantId == Guid.Empty || artistTenantId == Guid.Empty)
            throw new InvalidOperationException("An application requires resolved venue and artist tenants.");

        ArtistId = artistId;
        OpportunityId = opportunityId;
        DealType = dealType;
        VenueTenantId = venueTenantId;
        ArtistTenantId = artistTenantId;
    }

    public void Accept(BookingEntity booking) => Booking = booking;

    internal void RecordPaymentVerified() => PaymentVerification = PaymentVerification.Verified;

    internal void RecordPaymentFailed() => PaymentVerification = PaymentVerification.Failed;

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

    private StandardApplication(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId)
        : base(artistId, opportunityId, dealType, venueTenantId, artistTenantId) { }

    public static StandardApplication Create(
        int artistId,
        int opportunityId,
        DealType dealType,
        Guid venueTenantId,
        Guid artistTenantId) =>
        new(artistId, opportunityId, dealType, venueTenantId, artistTenantId);
}

public sealed class PrepaidApplication : ApplicationEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private PrepaidApplication() { }

    private PrepaidApplication(
        int artistId,
        int opportunityId,
        DealType dealType,
        string paymentMethodId,
        Guid venueTenantId,
        Guid artistTenantId)
        : base(artistId, opportunityId, dealType, venueTenantId, artistTenantId)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static PrepaidApplication Create(
        int artistId,
        int opportunityId,
        DealType dealType,
        string paymentMethodId,
        Guid venueTenantId,
        Guid artistTenantId) =>
        new(artistId, opportunityId, dealType, paymentMethodId, venueTenantId, artistTenantId);
}
