namespace Concertable.B2B.User.Contracts;

public sealed record ManagerDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? Avatar { get; init; }
}
