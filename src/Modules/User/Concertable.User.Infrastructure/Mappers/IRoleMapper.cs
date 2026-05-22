namespace Concertable.User.Infrastructure.Mappers;

internal interface IRoleMapper
{
    Role Role { get; }
    Task<IUser> ToDtoAsync(UserEntity user);
}
