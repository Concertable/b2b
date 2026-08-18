using Concertable.B2B.Booking.Api.Controllers;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Booking.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBookingApi(IConfiguration configuration)
        {
            services.AddBookingModule(configuration);
            services.AddControllers().AddInternalControllers(typeof(ContractController).Assembly);
            return services;
        }
    }
}
