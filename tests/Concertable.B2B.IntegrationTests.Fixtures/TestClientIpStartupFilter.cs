using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/* The in-memory TestServer never sets Connection.RemoteIpAddress, but recording an e-signature
   (ClientContextAccessor) requires a client IP. Stamp loopback at the front of the pipeline so the
   Apply/Accept flows behave like a real request. */
internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
            await nextMiddleware();
        });
        next(app);
    };
}
