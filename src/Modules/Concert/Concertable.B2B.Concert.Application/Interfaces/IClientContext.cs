using System.Net;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Ambient facts about the calling client (consent evidence), resolved per request like
/// <see cref="Concertable.Kernel.Identity.ICurrentUser"/>. Null outside an HTTP request.
/// </summary>
internal interface IClientContext
{
    IPAddress? IpAddress { get; }
    string? UserAgent { get; }
}
