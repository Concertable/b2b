using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Contracts;

public interface IApplicationModule
{
    bool RequiresApplyCheckout(DealType dealType);
    bool RequiresAcceptCheckout(DealType dealType);
}
