namespace Concertable.B2B.User.Infrastructure.Mappers;

internal interface IUserMapper
{
    Task<UserDto?> ToDtoAsync(UserEntity user);
}
