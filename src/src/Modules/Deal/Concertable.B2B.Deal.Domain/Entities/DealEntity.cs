using Concertable.Kernel;

namespace Concertable.B2B.Deal.Domain.Entities;

public abstract class DealEntity : IIdEntity, ITenantScoped
{
    protected DealEntity() { }

    public int Id { get; private set; }
    public Guid TenantId { get; set; }
    public PaymentMethod PaymentMethod { get; protected set; }
    public abstract DealType ContractType { get; }
}
