
namespace Concertable.User.Application.Interfaces;

internal interface IUserService
{
    Task<IUser> SaveLocationAsync(double latitude, double longitude);
}
