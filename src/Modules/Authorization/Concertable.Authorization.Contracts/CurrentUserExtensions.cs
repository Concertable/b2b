namespace Concertable.Authorization.Contracts;

public static class CurrentUserExtensions
{
    public static Guid GetId(this ICurrentUser currentUser) =>
        currentUser.Id ?? throw new UnauthorizedAccessException("User not authenticated.");
}
