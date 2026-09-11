using System.Net;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Infrastructure.Context;

internal sealed class ClientContextAccessor(IHttpContextAccessor httpContextAccessor) : IClientContext
{
    public IPAddress IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress
        ?? throw new InvalidOperationException("Cannot record an e-signature without a client IP address");

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
}
