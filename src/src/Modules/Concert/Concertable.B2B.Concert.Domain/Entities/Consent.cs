namespace Concertable.B2B.Concert.Domain.Entities;

public sealed record Consent(Guid UserId, DateTime AtUtc, string? Ip, string? UserAgent);
