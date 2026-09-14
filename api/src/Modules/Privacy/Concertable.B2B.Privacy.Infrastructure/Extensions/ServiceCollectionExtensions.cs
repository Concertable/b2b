using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Privacy.Domain.Lifecycle;
using Concertable.B2B.Privacy.Infrastructure.Data;
using Concertable.B2B.Privacy.Infrastructure.Data.Seeders;
using Concertable.B2B.Privacy.Infrastructure.Repositories;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Privacy.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrivacyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PrivacyDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>()));

        services.AddSingleton<PrivacyConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<PrivacyConfigurationProvider>());

        services.AddSingleton<ErasureStateMachine>();
        services.AddScoped<ISubjectErasureRepository, SubjectErasureRepository>();
        services.AddScoped<ISubjectObligationChecker, SubjectObligationChecker>();
        services.AddScoped<ISubjectErasureService, SubjectErasureService>();
        services.AddScoped<ISubjectExporter, SubjectExporter>();

        return services;
    }

    public static IServiceCollection AddPrivacyDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, PrivacyDevSeeder>();
        return services;
    }

    public static IServiceCollection AddPrivacyTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, PrivacyTestSeeder>();
        return services;
    }
}
