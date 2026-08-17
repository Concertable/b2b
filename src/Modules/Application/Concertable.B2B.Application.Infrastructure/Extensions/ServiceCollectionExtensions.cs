using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationModule() => services;
    }
}
