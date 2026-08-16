namespace Concertable.B2B.User.Application.Requests;

internal sealed record CreateAdminInvitationRequest
{
    public required string Email { get; init; }
}
