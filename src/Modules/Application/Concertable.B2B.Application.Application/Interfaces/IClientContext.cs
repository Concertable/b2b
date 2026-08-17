using System.Net;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IClientContext
{
    IPAddress IpAddress { get; }
    string? UserAgent { get; }
}
