using System.Net;

namespace Concertable.B2B.RequestContext;

public interface IClientContext
{
    IPAddress IpAddress { get; }
    string? UserAgent { get; }
}
