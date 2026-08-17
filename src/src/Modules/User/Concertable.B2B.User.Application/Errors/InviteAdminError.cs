using Dunet;

namespace Concertable.B2B.User.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record InviteAdminError : IError
{
    public ErrorDefinition Definition => this switch
    {
        AlreadyAdmin =>
            ErrorDefinition.Conflict<AlreadyAdmin>("This person is already an admin."),
        InvitationPending =>
            ErrorDefinition.Conflict<InvitationPending>("An invitation for this email is already pending."),
        Unauthenticated =>
            ErrorDefinition.Forbidden<Unauthenticated>("No authenticated user was found.")
    };

    [ErrorCode("admin.invite_already_admin")]
    public partial record AlreadyAdmin;

    [ErrorCode("admin.invite_already_pending")]
    public partial record InvitationPending;

    [ErrorCode("admin.invite_unauthenticated")]
    public partial record Unauthenticated;
}
