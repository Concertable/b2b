using Concertable.B2B.Booking.Api.Controllers;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookingApi(this IServiceCollection services)
    {
        services.AddControllers().AddInternalControllers(typeof(ContractController).Assembly);
        return services;
    }
}
