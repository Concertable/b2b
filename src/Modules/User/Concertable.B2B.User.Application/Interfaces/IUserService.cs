
namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<UserDto> SaveLocationAsync(double latitude, double longitude);
}
