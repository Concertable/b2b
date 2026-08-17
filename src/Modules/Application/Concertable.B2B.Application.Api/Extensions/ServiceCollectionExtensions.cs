using Concertable.B2B.Application.Api.Controllers;
using Concertable.B2B.Application.Api.Mappers;
using Concertable.B2B.Application.Api.Validators;
using Concertable.Shared.Api.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationApi(this IServiceCollection services)
    {
        services.AddScoped<IApplicationResponseMapper, ApplicationResponseMapper>();
        services.AddValidatorsFromAssemblyContaining<ApplyRequestValidator>(includeInternalTypes: true);
        services.AddControllers().AddInternalControllers(typeof(ApplicationController).Assembly);
        return services;
    }
}
