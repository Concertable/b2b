namespace Concertable.B2B.Admin.Application.Requests;

internal sealed record CreateAdminInvitationRequest
{
    public required string Email { get; init; }
}
