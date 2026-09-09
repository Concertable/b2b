using Concertable.B2B.Workers;
using Concertable.Testing.Architecture;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Concertable.B2B.StartupTests;

public sealed class WorkerHostTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
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
}
