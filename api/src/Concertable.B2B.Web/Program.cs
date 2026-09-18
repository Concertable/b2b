using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddB2BWebHost();

var app = builder.Build();
await app.UseB2BWebHost();

/* The stance the host's own background work runs under — outbox dispatch, queued message handling and
   projection handlers, none of which has a request or a member to be authorised as. It is established here
   rather than inside UseB2BWebHost because an async method's ambient changes do not flow back to its caller,
   and deliberately never disposed: it is the process's stance for as long as the process runs. Requests
   re-establish the interactive stance in TenantResolutionMiddleware, so none of them inherits this. */
app.Services.GetRequiredService<IExecutionScopeActivator>().Enter(ExecutionPurpose.System);

app.Run();

public sealed partial class Program
{ }
