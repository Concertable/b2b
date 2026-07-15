using System.ComponentModel;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

[DisplayName("Booking")]
public abstract class BookingEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    public int ApplicationId { get; private set; }
    public DealType DealType { get; private set; }
    public ApplicationEntity Application { get; set; } = null!;
    public ConcertEntity? Concert { get; private set; }

    protected BookingEntity() { }

    protected BookingEntity(int applicationId, DealType dealType)
    {
        ApplicationId = applicationId;
        DealType = dealType;
    }

    public void Confirm(ConcertEntity concert) => Concert = concert;
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(int applicationId, DealType dealType)
        : base(applicationId, dealType) { }

    public static StandardBooking Create(int applicationId, DealType dealType) =>
        new(applicationId, dealType);
}

public sealed class DeferredBooking : BookingEntity
{
    public string PaymentMethodId { get; private set; } = null!;

    private DeferredBooking() { }

    private DeferredBooking(int applicationId, DealType dealType, string paymentMethodId)
        : base(applicationId, dealType)
    {
        PaymentMethodId = paymentMethodId;
    }

    public static DeferredBooking Create(int applicationId, DealType dealType, string paymentMethodId) =>
        new(applicationId, dealType, paymentMethodId);
}
