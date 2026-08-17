using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

[DisplayName(DisplayNames.Booking)]
public abstract class BookingEntity : IIdEntity, IVenueArtistTenantScoped, IEventRaiser
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid OperationId { get; private set; }
    public int ApplicationId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public DealType DealType { get; private set; }

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
        DealType = application.DealType;
        VenueTenantId = application.VenueTenantId;
        ArtistTenantId = application.ArtistTenantId;
    }

    public void Confirm(DateRange period, string venueName, string artistName) =>
        events.Raise(new BookingConfirmedDomainEvent(VenueTenantId, venueName, ArtistTenantId, artistName, period));
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(AcceptedApplication application)
        : base(application) { }

    public static StandardBooking Create(AcceptedApplication application) => new(application);
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private DeferredBooking() { }

    private DeferredBooking(AcceptedApplication application, string paymentMethodId)
        : base(application)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static DeferredBooking Create(AcceptedApplication application, string paymentMethodId) =>
        new(application, paymentMethodId);
}
