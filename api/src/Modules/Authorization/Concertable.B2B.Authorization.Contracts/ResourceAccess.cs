namespace Concertable.B2B.Authorization.Contracts;

public enum ResourceAccess
{
    // Ordering is significant; broader access must have a larger value.
    None = 0,
    Assigned = 1,
    All = 2,
}
