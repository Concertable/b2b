using System.Net;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ClientContextAccessor : IClientContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ClientContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public IPAddress? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
}
