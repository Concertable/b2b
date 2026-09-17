using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertCommandReceipt : ResourceCommandReceipt
{
    public const string ShareSummaryOperation = "concert.share_summary";

    private ConcertCommandReceipt() { }

    public static ConcertCommandReceipt Record(
        Guid issuedByTenantId, string operation, Guid requestId, string payloadHash, string outcome, DateTime at)
    {
        var receipt = new ConcertCommandReceipt();
        receipt.Initialize(issuedByTenantId, operation, requestId, payloadHash, outcome, at);
        return receipt;
    }
}
