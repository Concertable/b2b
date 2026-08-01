namespace Concertable.B2B.User.Application.Errors;

internal sealed record SaveLocationError : IError
{
    public ErrorDefinition Definition => ErrorDefinition.Unauthenticated(
        "user.location_unauthenticated",
        "The current user was not found.");
}
