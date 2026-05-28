
namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<UserBase> SaveLocationAsync(double latitude, double longitude);
}
