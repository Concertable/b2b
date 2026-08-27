using Concertable.B2B.Application.Contracts;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    TStrategy Create(DealType dealType);
}