using Concertable.Kernel;

namespace Concertable.B2B.User.Infrastructure.Mappers;

internal sealed class UserMapper : IUserMapper
{
    public Task<UserDto?> ToDtoAsync(UserEntity user) => Task.FromResult<UserDto?>(new UserDto
    {
        Id = user.Id,
        Email = user.Email,
        Latitude = user.Location.ToLatitude(),
        Longitude = user.Location.ToLongitude(),
        County = user.Address?.County,
        Town = user.Address?.Town,
        IsEmailVerified = true,
    });
}
