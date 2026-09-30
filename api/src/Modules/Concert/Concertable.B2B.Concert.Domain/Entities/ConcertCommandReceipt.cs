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
    public CommandPayloadHash PayloadHash { get; private set; } = null!;
    public string Outcome { get; private set; } = null!;
    public DateTime RecordedAtUtc { get; private set; }

    public static ConcertCommandReceipt Record(
        Guid issuedByTenantId, string operation, Guid requestId, CommandPayloadHash payloadHash, string outcome, DateTime at) => new()
        {
            Id = Guid.NewGuid(),
            IssuedByTenantId = issuedByTenantId,
            Operation = operation,
            RequestId = requestId,
            PayloadHash = payloadHash ?? throw new ArgumentNullException(nameof(payloadHash)),
            Outcome = outcome,
            RecordedAtUtc = at
        };

    public bool Matches(CommandPayloadHash payloadHash) => PayloadHash ==
        (payloadHash ?? throw new ArgumentNullException(nameof(payloadHash)));
}
