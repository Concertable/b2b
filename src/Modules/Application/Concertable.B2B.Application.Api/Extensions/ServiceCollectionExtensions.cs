using Concertable.B2B.Application.Api.Controllers;
using Concertable.B2B.Application.Api.Mappers;
using Concertable.B2B.Application.Api.Validators;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationApi(IConfiguration configuration)
        {
            services.AddApplicationModule(configuration);
            services.AddScoped<IApplicationResponseMapper, ApplicationResponseMapper>();
            services.AddValidatorsFromAssemblyContaining<ApplyRequestValidator>(includeInternalTypes: true);
            services.AddControllers().AddInternalControllers(typeof(ApplicationController).Assembly);
            return services;
        }

        public IServiceCollection AddApplicationDevSeeder() =>
            Concertable.B2B.Application.Infrastructure.Extensions.ServiceCollectionExtensions
                .AddApplicationDevSeeder(services);
    }
}
