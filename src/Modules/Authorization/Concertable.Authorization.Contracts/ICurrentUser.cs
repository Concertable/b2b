namespace Concertable.Authorization.Contracts;

public interface ICurrentUser
{
    Guid? Id { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
