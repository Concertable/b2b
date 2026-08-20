using Concertable.B2B.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Concertable.B2B.Workers.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_ShouldBuildValidatedServiceProvider()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Integration
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:B2B"] = "Server=localhost;Database=Concertable;User Id=sa;Password=Test123!;TrustServerCertificate=True",
            ["services:payment-web:https:0"] = "https://localhost:7101"
        });

        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
