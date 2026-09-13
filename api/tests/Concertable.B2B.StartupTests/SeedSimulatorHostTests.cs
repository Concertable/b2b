using Concertable.B2B.Seed.Simulator;
using Concertable.Testing.Architecture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Concertable.B2B.StartupTests;

public sealed class SeedSimulatorHostTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        builder.AddSeedSimulatorHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(HostExtensions).Assembly]
        });
        var invalidBuilder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSeedSimulatorHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }
}
