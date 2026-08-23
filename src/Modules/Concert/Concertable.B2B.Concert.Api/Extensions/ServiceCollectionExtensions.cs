using Concertable.B2B.Concert.Api.Controllers;
using Concertable.B2B.Concert.Api.Validators;
using Concertable.B2B.Concert.Application.Validators;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddConcertApi(IConfiguration configuration)
        {
            services.AddConcertModule(configuration);
            services.AddConcertDevSeeder();
            services.AddValidatorsFromAssemblyContaining<ESignatureRequestValidator>(includeInternalTypes: true);
            services.AddValidatorsFromAssemblyContaining<UpdateConcertRequestValidator>(includeInternalTypes: true);
            services.AddControllers()
                .AddInternalControllers(typeof(ConcertController).Assembly);
            return services;
        }
    }
}
