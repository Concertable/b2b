using System.Net;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Ambient facts about the calling client (consent evidence), resolved per request like
/// <see cref="Concertable.Kernel.Identity.ICurrentUser"/>.
/// </summary>
internal interface IClientContext
{
    /// <summary>
    /// The calling client's peer IP. A live sign happens on an HTTP request whose socket always has a
    /// remote address, so an absent IP is a server misconfiguration — this fails closed rather than
    /// record a signature with no origin. Throws <see cref="InvalidOperationException"/> if absent.
    /// </summary>
    IPAddress IpAddress { get; }

    string? UserAgent { get; }
}
