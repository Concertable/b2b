using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = B2BWorkerHost.CreateBuilder(args).Build();

host.Run();
