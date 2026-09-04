using Concertable.B2B.Admin.Contracts;
using Concertable.B2B.Web;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.B2B.StartupTests;

public sealed class WebHostTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(B2BWebHostExtensions).Assembly]
        });
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.False(jwtOptions.RequireHttpsMetadata);
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddB2BWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void ProductionEnvironment_RequiresHttpsMetadata()
    {
        var arguments = CompositionTestArguments.Create();
        arguments[0] = "--environment=Production";
        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddB2BWebHost();
        using var app = builder.Build();
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(jwtOptions.RequireHttpsMetadata);
    }

    [Fact]
    public void MissingAdminModule_FailsWithUnresolvedDependency()
    {
        // IAdminModule's only consumer is UserController.Me() — Web-hosted, not Workers.
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        builder.Services.RemoveAll<IAdminModule>();
        var exception = Record.Exception(() =>
        {
            using var app = builder.Build();
            builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
            {
                RootAssemblies = [typeof(B2BWebHostExtensions).Assembly]
            });
        });
        Assert.NotNull(exception);
        Assert.Contains(typeof(IAdminModule).FullName!, exception.ToString(), StringComparison.Ordinal);
    }
}
