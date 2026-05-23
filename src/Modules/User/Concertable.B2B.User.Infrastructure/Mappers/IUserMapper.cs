namespace Concertable.B2B.User.Infrastructure.Mappers;

internal interface IUserMapper
{
    Task<IUser?> ToDtoAsync(UserEntity user);
}
