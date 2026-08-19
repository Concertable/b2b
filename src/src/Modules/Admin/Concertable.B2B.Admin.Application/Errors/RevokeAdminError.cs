using Dunet;

namespace Concertable.B2B.Admin.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeAdminError : IError
{
    public ErrorDefinition Definition => this switch
    {
        AdminNotFound(var sub) =>
            ErrorDefinition.NotFound<AdminNotFound>($"Admin {sub} was not found."),
        LastAdmin =>
            ErrorDefinition.Conflict<LastAdmin>("The last admin cannot be removed.")
    };

    [ErrorCode("admin.revoke_admin_not_found")]
    public partial record AdminNotFound(Guid Sub);

    [ErrorCode("admin.revoke_admin_last_admin")]
    public partial record LastAdmin;
}
