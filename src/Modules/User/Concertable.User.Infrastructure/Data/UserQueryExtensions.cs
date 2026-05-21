namespace Concertable.User.Infrastructure.Data;

internal static class UserQueryExtensions
{
    public static IQueryable<UserEntity> WhereCredentials(this IQueryable<UserEntity> users, string email, Role role) =>
        users.Where(u => u.Email == email && u.Role == role);
}
