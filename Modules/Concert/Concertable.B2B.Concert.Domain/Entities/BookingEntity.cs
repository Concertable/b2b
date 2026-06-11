using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

public abstract class BookingEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; set; }
    public Guid ArtistTenantId { get; set; }
    public int ApplicationId { get; private set; }
    public ContractType ContractType { get; private set; }
    public ApplicationEntity Application { get; set; } = null!;
    public ConcertEntity? Concert { get; private set; }

    protected BookingEntity() { }

    protected BookingEntity(int applicationId, ContractType contractType)
    {
        ApplicationId = applicationId;
        ContractType = contractType;
    }

    public void Confirm(ConcertEntity concert) => Concert = concert;
}

public sealed class StandardBooking : BookingEntity
{
    private StandardBooking() { }

    private StandardBooking(int applicationId, ContractType contractType)
        : base(applicationId, contractType) { }

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

    public static DeferredBooking Create(int applicationId, ContractType contractType, string paymentMethodId) =>
        new(applicationId, contractType, paymentMethodId);
}
