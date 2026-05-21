
namespace Concertable.User.Application.Interfaces.Auth;

internal interface IUserRegister
{
    Task RegisterAsync(string email, string passwordHash, Role role);
}
