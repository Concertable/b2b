using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = B2BWorkerHost.CreateBuilder(args).Build();

// This process serves no requests; everything it runs is the platform acting for itself.
host.Services.GetRequiredService<IExecutionScopeActivator>().Enter(ExecutionPurpose.System);

host.Run();
