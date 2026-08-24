using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel;

namespace Concertable.B2B.Application.Domain.Entities;

[DisplayName(DisplayNames.Application)]
public abstract class ApplicationEntity : IIdEntity, IVenueArtistTenantScoped, IEventRaiser
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    internal ApplicationState State { get; private set; } = ApplicationState.Applied;
    internal VerifyPaymentEntity? VerifyPayment { get; private set; }
    internal VerifyPayment? Verification => VerifyPayment?.ToContract();
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public DealType DealType { get; private set; }
    public Guid? AcceptanceOperationId { get; private set; }

    public Signature ArtistESignature { get; private set; } = null!;
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

    public Guid BeginAcceptance() => BeginAcceptance(Guid.NewGuid());

    public Guid BeginAcceptance(Guid operationId)
    {
        if (operationId == Guid.Empty)
            throw new ArgumentException("An acceptance operation id is required.", nameof(operationId));

        AcceptanceOperationId ??= operationId;
        if (AcceptanceOperationId != operationId)
            throw new InvalidOperationException("The application already belongs to another acceptance operation.");

        return AcceptanceOperationId.Value;
    }

    internal void RecordVerifyPayment(VerifyPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.ApplicationId != Id)
            throw new InvalidOperationException(
                $"Verify payment for application {payment.ApplicationId} cannot be recorded against application {Id}.");

        var existing = VerifyPayment?.ToContract();
        if (existing == payment)
            return;
        if (existing?.ProviderTransactionId == payment.ProviderTransactionId)
            throw new InvalidOperationException(
                $"Verify payment {payment.ProviderTransactionId} cannot change its recorded outcome.");

        VerifyPayment = VerifyPaymentEntity.Create(payment);
        events.Raise(payment);
    }

    public void RecordArtistESignature(Signature eSignature, string termsFingerprint)
    {
        ArtistESignature = eSignature;
        TermsFingerprint = termsFingerprint;
    }

    internal void Accept(AcceptedApplication application)
    {
        if (application.ApplicationId != Id || application.OperationId != AcceptanceOperationId)
            throw new InvalidOperationException("Accepted application facts do not match the application transition.");

        Transition(ApplicationState.Accepted);
        events.Raise(new ApplicationAcceptedDomainEvent(application));
    }
    internal void Reject() => Transition(ApplicationState.Rejected);
    internal void Withdraw() => Transition(ApplicationState.Withdrawn);

    private void Transition(ApplicationState next)
    {
        if (State != ApplicationState.Applied)
            throw new InvalidOperationException($"Application {Id} cannot transition from {State} to {next}.");

        State = next;
    }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public void NotifyCounterparty(ApplicationNotification kind)
    {
        var recipient = kind is ApplicationNotification.Applied or ApplicationNotification.Withdrawn
            ? VenueTenantId
            : ArtistTenantId;
        events.Raise(new ApplicationCounterpartyNotifiedDomainEvent(recipient, kind));
    }
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
