using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ClientContextAccessor : IClientContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ClientContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
}
