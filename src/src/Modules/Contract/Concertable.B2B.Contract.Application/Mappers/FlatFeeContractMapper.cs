using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;

namespace Concertable.B2B.Contract.Application.Mappers;

internal sealed class FlatFeeContractMapper : IContractMapper
{
    public IContract ToContract(ContractEntity entity)
    {
        var e = (FlatFeeContractEntity)entity;
        return new FlatFeeContract
        {
            Id = e.Id,
            PaymentMethod = e.PaymentMethod,
            Fee = e.Fee
        };
    }

    public ContractEntity ToEntity(IContract contract)
    {
        var c = (FlatFeeContract)contract;
        return FlatFeeContractEntity.Create(c.Fee, c.PaymentMethod);
    }
}
