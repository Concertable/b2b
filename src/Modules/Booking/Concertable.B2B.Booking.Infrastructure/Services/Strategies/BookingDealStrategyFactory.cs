using Concertable.B2B.Booking.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.Infrastructure.Services.Strategies;

internal sealed class BookingDealStrategyFactory<TStrategy> : IBookingDealStrategyFactory<TStrategy>
    where TStrategy : class
{
    private readonly IKeyedServiceProvider services;

    public BookingDealStrategyFactory(IKeyedServiceProvider services)
    {
        this.services = services;
    }

    public TStrategy Create(DealType dealType) =>
        this.services.GetRequiredKeyedService<TStrategy>(dealType);
}
