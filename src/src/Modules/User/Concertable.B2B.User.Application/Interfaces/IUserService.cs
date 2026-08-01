
using Concertable.B2B.User.Application.Errors;

namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<Result<UserDto, SaveLocationError>> SaveLocationAsync(double latitude, double longitude);
}
