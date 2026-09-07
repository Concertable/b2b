using Concertable.B2B.Deal.Domain;
using Concertable.B2B.Enums;

namespace Concertable.B2B.Application.Contracts;

public static class ApplicationCheckoutExtensions
{
    extension(DealType dealType)
    {
        public bool RequiresApplyCheckout() => dealType == DealType.VenueHire;

        public bool RequiresAcceptCheckout() => dealType != DealType.VenueHire;
    }
}
