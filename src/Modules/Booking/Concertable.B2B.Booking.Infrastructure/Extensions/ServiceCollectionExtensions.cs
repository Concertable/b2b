using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBookingModule() => services;
    }
}
