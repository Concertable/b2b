using System.Net;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ClientContextAccessor(IHttpContextAccessor httpContextAccessor) : IClientContext
{
    public IPAddress IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress
        ?? throw new InvalidOperationException("Cannot record an e-signature without a client IP address");

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
}
