namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A gap-free counter you can allocate the next number from. The generic contract concrete per-subject
/// counters implement (e.g. <see cref="InvoiceSequenceEntity"/>), so sequence machinery works against the
/// abstraction rather than a specific counter.
/// </summary>
public interface ISequence
{
    long Allocate();
}
