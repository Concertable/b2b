using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VersusPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IContract contract)
    {
        var c = (VersusContract)contract;
        return new GuaranteedDoorPayment(c.Guarantee, c.ArtistDoorPercent);
    }
}
