using System.Net;

namespace Concertable.B2B.Application.Domain.ValueObjects;

internal sealed record Signature(
    Guid UserId,
    DateTime AtUtc,
    IPAddress Ip,
    string? UserAgent,
    string SignatoryName,
    string? DrawnSignatureImage);
