namespace Concertable.User.Infrastructure.Mappers;

internal interface IUserMapper
{
    Task<IUser?> ToDtoAsync(UserEntity user);
}
