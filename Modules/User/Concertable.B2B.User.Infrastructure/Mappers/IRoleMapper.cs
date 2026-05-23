namespace Concertable.B2B.User.Infrastructure.Mappers;

internal interface IRoleMapper
{
    Role Role { get; }
    Task<IUser> ToDtoAsync(UserEntity user);
}
