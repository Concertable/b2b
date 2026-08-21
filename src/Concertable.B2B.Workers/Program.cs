using Concertable.B2B.Workers;
using Microsoft.Extensions.Hosting;

var builder = B2BWorkerHost.CreateBuilder(args);
builder.Build().Run();
