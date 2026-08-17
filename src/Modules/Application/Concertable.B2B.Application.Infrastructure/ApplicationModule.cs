using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Infrastructure;

internal sealed class ApplicationModule : IApplicationModule
{
    public bool RequiresApplyCheckout(DealType dealType) => dealType == DealType.VenueHire;

    public bool RequiresAcceptCheckout(DealType dealType) => dealType != DealType.VenueHire;
}
