using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// A gap-free counter you can allocate the next number from.
/// </summary>
public interface ISequence
{
    long Allocate();
}

/// <summary>
/// A tenant-keyed, self-creating counter. The self type lets the generic sequence repository create the
/// per-tenant counter on first allocation without knowing the concrete type.
/// </summary>
public interface ISequence<TSelf> : ISequence, ITenant
    where TSelf : class, ISequence<TSelf>
{
    static abstract TSelf Create(Guid id);
}
