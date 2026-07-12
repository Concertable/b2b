using System.Net;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A self-hosted, named e-signature: the typed full name (and optional drawn image) that binds a
/// specific human to the agreement, plus the ambient attribution the server records — never the
/// client. Advanced-tier: <paramref name="SignatoryName"/> is the required core; the drawn image
/// only adds perceived weight.
/// </summary>
public sealed record ESignature(
    Guid UserId,
    DateTime AtUtc,
    IPAddress? Ip,
    string? UserAgent,
    string SignatoryName,
    string? DrawnSignatureImage);
