using Concertable.Payment.Client;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/// <summary>
/// In-memory stand-in for the escrow rows the real Payment service would persist. B2B treats Payment
/// as an external service (mocked at the <see cref="IEscrowClient"/> boundary), so escrow observability
/// for assertions lives here rather than in Payment's DbContext.
/// </summary>
public sealed class EscrowStore : IResettable
{
    private readonly List<EscrowRecord> escrows = [];
    private int nextId;

    public IReadOnlyList<EscrowRecord> Escrows => escrows;

    public int Add(EscrowRecord escrow)
    {
        escrows.Add(escrow);
        return ++nextId;
    }

    public void Reset()
    {
        escrows.Clear();
        nextId = 0;
    }
}

public sealed record EscrowRecord(
    int BookingId,
    Guid FromOwnerId,
    Guid ToOwnerId,
    long Amount,
    string ChargeId,
    EscrowStatus Status);
