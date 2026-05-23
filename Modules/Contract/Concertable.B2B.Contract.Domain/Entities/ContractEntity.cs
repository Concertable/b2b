using Concertable.Kernel;

namespace Concertable.B2B.Contract.Domain.Entities;

public abstract class ContractEntity : IIdEntity
{
    protected ContractEntity() { }

    public int Id { get; private set; }
    public PaymentMethod PaymentMethod { get; protected set; }
    public abstract ContractType ContractType { get; }
}
