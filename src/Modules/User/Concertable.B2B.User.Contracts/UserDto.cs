namespace Concertable.B2B.User.Contracts;

public record ManagerDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? Avatar { get; init; }
}
