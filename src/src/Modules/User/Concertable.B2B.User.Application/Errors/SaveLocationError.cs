using Dunet;

namespace Concertable.B2B.User.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record SaveLocationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        UserNotFound =>
            ErrorDefinition.Unauthenticated<UserNotFound>(
                "The current user was not found.")
    };

    [ErrorCode("user.location_unauthenticated")]
    public partial record UserNotFound;
}
