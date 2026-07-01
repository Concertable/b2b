using Concertable.Kernel;

namespace Concertable.B2B.User.Infrastructure.Mappers;

internal sealed class AdminMapper : IRoleMapper
{
    public Role Role => Role.Admin;

    public Task<UserBase> ToDtoAsync(UserEntity user) => Task.FromResult<UserBase>(new AdminDto
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
