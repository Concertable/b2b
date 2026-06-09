using Concertable.Kernel;

namespace Concertable.B2B.Contract.Domain.Entities;

public abstract class ContractEntity : IIdEntity, ITenantScoped
{
    protected ContractEntity() { }

    public int Id { get; private set; }
    public Guid TenantId { get; set; }
    public PaymentMethod PaymentMethod { get; protected set; }
    public abstract ContractType ContractType { get; }
}
