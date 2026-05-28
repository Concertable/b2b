namespace Concertable.B2B.User.Infrastructure.Mappers;

internal interface IRoleMapper
{
    Role Role { get; }
    Task<UserBase> ToDtoAsync(UserEntity user);
}
