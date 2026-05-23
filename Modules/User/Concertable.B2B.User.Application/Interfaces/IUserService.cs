
namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserService
{
    Task<IUser> SaveLocationAsync(double latitude, double longitude);
}
