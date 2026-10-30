using Concertable.Kernel;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertCommandReceipt : IGuidEntity
{
    public const string ShareSummaryOperation = "concert.share_summary";

    private ConcertCommandReceipt() { }

    public Guid Id { get; private set; }
    public Guid IssuedByTenantId { get; private set; }
    public string Operation { get; private set; } = null!;
    public Guid RequestId { get; private set; }
    public IdempotencyHash IdempotencyHash { get; private set; }
    public string Outcome { get; private set; } = null!;
    public DateTime RecordedAtUtc { get; private set; }

    public static ConcertCommandReceipt Record(
        Guid issuedByTenantId, string operation, Guid requestId, IdempotencyHash idempotencyHash, string outcome, DateTime at) => new()
        {
            Id = Guid.NewGuid(),
            IssuedByTenantId = issuedByTenantId,
            Operation = operation,
            RequestId = requestId,
            IdempotencyHash = idempotencyHash != default
                ? idempotencyHash
                : throw new ArgumentException("An idempotency hash is required.", nameof(idempotencyHash)),
            Outcome = outcome,
            RecordedAtUtc = at
        };

    public bool Matches(IdempotencyHash idempotencyHash) => IdempotencyHash ==
        (idempotencyHash != default
            ? idempotencyHash
            : throw new ArgumentException("An idempotency hash is required.", nameof(idempotencyHash)));
}
