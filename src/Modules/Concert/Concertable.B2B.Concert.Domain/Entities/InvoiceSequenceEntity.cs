namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// The per-supplier invoice counter — one row per issuing tenant, holding the next number to allocate.
/// Allocation happens inside the settlement transaction so the number and the invoice commit together;
/// that shared commit is what makes the sequence gap-free. <see cref="RowVersion"/> serialises concurrent
/// allocations for the same supplier (the loser retries on the next sweep, so no number is ever skipped).
/// </summary>
public sealed class InvoiceSequenceEntity : ISequence<InvoiceSequenceEntity>
{
    public Guid TenantId { get; private set; }
    public long NextNumber { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private InvoiceSequenceEntity() { }

    public static InvoiceSequenceEntity Create(Guid tenantId) => new() { TenantId = tenantId, NextNumber = 1 };

    public long Allocate()
    {
        var number = NextNumber;
        NextNumber = number + 1;
        return number;
    }
}
