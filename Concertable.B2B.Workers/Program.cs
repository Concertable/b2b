using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Concertable.B2B.Workers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
}));

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddSingleton<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp)
    .AddInfrastructure(builder.Configuration);

builder.Build().Run();
