using System.Net;

namespace Concertable.B2B.Application.Contracts;

public sealed record ContractSignature(
    Guid UserId,
    DateTime AtUtc,
    IPAddress Ip,
    string? UserAgent,
    string SignatoryName,
    string? DrawnSignatureImage);
