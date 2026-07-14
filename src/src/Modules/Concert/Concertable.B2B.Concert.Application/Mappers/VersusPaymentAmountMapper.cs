using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VersusPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(IDeal contract)
    {
        var c = (VersusDeal)contract;
        return new GuaranteedDoorPayment(c.Guarantee, c.ArtistDoorPercent);
    }
}
