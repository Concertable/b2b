
using Concertable.B2B.User.Application.Errors;

namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<Result<UserBase, SaveLocationError>> SaveLocationAsync(double latitude, double longitude);
}
