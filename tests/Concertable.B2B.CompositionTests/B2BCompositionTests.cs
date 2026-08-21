using Concertable.B2B.Admin.Contracts;
using Concertable.B2B.Seed.Simulator;
using Concertable.B2B.Web;
using Concertable.B2B.Workers;
using Concertable.Composition.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Concertable.B2B.CompositionTests;

public sealed class B2BCompositionTests
{
    [Fact]
    public void Web_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(B2BWebHostExtensions).Assembly]
        });
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddB2BWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Functions_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = B2BWorkerHost.CreateBuilder(CompositionTestArguments.Create());
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(B2BWorkerHost).Assembly],
            IsFunction = method => method.IsDefined(typeof(FunctionAttribute), inherit: false)
        });
        var invalidBuilder = B2BWorkerHost.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Functions_MissingAdminModule_FailsWithUnresolvedDependency()
    {
        var builder = B2BWorkerHost.CreateBuilder(CompositionTestArguments.Create());
        builder.Services.RemoveAll<IAdminModule>();
        var exception = Record.Exception(() =>
        {
            using var app = builder.Build();
            builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
            {
                RootAssemblies = [typeof(B2BWorkerHost).Assembly],
                IsFunction = method => method.IsDefined(typeof(FunctionAttribute), inherit: false)
            });
        });
        Assert.NotNull(exception);
        Assert.Contains(typeof(IAdminModule).FullName!, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSimulator_ProductionGraphAndStrictValidation_AreValid()
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

    [Fact]
    public void AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        using var app = B2BAppHost.CreateBuilder([]).Build();
        var builder = B2BAppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }
}
