namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertCommandReceipt : IGuidEntity
{
    public const string ShareSummaryOperation = "concert.share_summary";

    private ConcertCommandReceipt() { }

    public Guid Id { get; private set; }
    public Guid IssuedByTenantId { get; private set; }
    public string Operation { get; private set; } = null!;
    public Guid RequestId { get; private set; }
    public Sha256Digest Fingerprint { get; private set; } = null!;
    public string Outcome { get; private set; } = null!;
    public DateTime RecordedAtUtc { get; private set; }

    public static ConcertCommandReceipt Record(
        Guid issuedByTenantId, string operation, Guid requestId, Sha256Digest fingerprint, string outcome, DateTime at) => new()
        {
            Id = Guid.NewGuid(),
            IssuedByTenantId = issuedByTenantId,
            Operation = operation,
            RequestId = requestId,
            Fingerprint = fingerprint,
            Outcome = outcome,
            RecordedAtUtc = at
        };

    public bool Matches(Sha256Digest fingerprint) => Fingerprint == fingerprint;
}
