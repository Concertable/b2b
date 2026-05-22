using System.Security.Claims;
using Concertable.Auth.Contracts;
using Concertable.User.Contracts;

namespace Concertable.User.Infrastructure.ProfileClaims;

internal sealed class UserProfileClaimsProvider : IProfileClaimsProvider
{
    private readonly IUserModule userModule;

    public UserProfileClaimsProvider(IUserModule userModule)
    {
        this.userModule = userModule;
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId)
    {
        var creds = await userModule.GetCredentialsByIdAsync(subjectId);
        if (creds is null)
            return [];

        var claims = new List<Claim>
        {
            new("email", creds.Email),
            new("email_verified", creds.IsEmailVerified ? "true" : "false", ClaimValueTypes.Boolean),
        };

        var user = await userModule.GetByIdAsync(subjectId);
        if (user is not null)
            claims.Add(new("role", user.Role.ToString()));

        return claims;
    }
}
