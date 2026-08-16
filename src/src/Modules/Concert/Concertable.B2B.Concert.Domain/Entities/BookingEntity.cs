using System.ComponentModel;
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
    public int ApplicationId { get; private set; }
    public ApplicationEntity Application { get; private set; } = null!;
    public ConcertEntity? Concert { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected BookingEntity() { }

    protected BookingEntity(ApplicationEntity application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.VenueTenantId == Guid.Empty || application.ArtistTenantId == Guid.Empty)
            throw new InvalidOperationException("A booking cannot inherit unresolved application tenants.");

        Application = application;
        ApplicationId = application.Id;
        VenueTenantId = application.VenueTenantId;
        ArtistTenantId = application.ArtistTenantId;
    }

    public void Confirm(ConcertEntity concert, string venueName, string artistName)
    {
        Concert = concert;
        events.Raise(new BookingConfirmedDomainEvent(VenueTenantId, venueName, ArtistTenantId, artistName, concert.Period));
    }
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(ApplicationEntity application)
        : base(application) { }

    public static StandardBooking Create(ApplicationEntity application) => new(application);
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private DeferredBooking() { }

    private DeferredBooking(ApplicationEntity application, string paymentMethodId)
        : base(application)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static DeferredBooking Create(ApplicationEntity application, string paymentMethodId) =>
        new(application, paymentMethodId);
}
