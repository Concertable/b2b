using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class VersusPaymentAmountMapper : IPaymentAmountMapper
{
    public IPaymentAmount ToPaymentAmount(DealDto deal)
    {
        var c = (VersusDealDto)deal;
        return new GuaranteedDoorPayment(c.Guarantee, c.ArtistDoorPercent);
    }
}
