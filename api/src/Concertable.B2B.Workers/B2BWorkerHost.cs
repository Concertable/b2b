using Concertable.ServiceDefaults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.B2B.Workers;

public static class B2BWorkerHost
{
    public static FunctionsApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);
        builder.ConfigureContainer(ServiceProviderValidation.CreateFactory());
        builder.AddAzureBlobServiceClient("blobs");
        builder.ConfigureFunctionsWebApplication();
        builder.Services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();
        builder.Services
            .AddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp)
            .AddInfrastructure(builder.Configuration, builder.Environment);
        return builder;
    }
}
