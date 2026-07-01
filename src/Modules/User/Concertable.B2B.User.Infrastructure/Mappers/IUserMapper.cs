namespace Concertable.B2B.User.Infrastructure.Mappers;

internal interface IUserMapper
{
    Task<UserBase?> ToDtoAsync(UserEntity user);
}
