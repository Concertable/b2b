namespace Concertable.B2B.Authorization.Contracts;

public enum ResourceKind
{
    Application = 1,
    Booking,
    Contract,
    Concert,
    Invoice,
    Conversation,
}

public sealed record ResourceAddress
{
    public ResourceKind Kind { get; }
    public int Id { get; }

    private ResourceAddress(ResourceKind kind, int id)
    {
        Kind = kind;
        Id = id;
    }

    public static ResourceAddress Create(ResourceKind kind, int id)
    {
        if (!Enum.IsDefined(kind) || id <= 0)
            throw new ArgumentOutOfRangeException(nameof(kind), "A known resource kind and positive ID are required.");

        return new ResourceAddress(kind, id);
    }
}
