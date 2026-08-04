using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Errors;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantErrorTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            AcceptInvitationError.NotFound(Id),
            "tenant.accept_invitation_not_found",
            $"Invitation {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            AcceptInvitationError.EmailMismatch,
            "tenant.accept_invitation_email_mismatch",
            "This invitation was issued to a different email address.",
            ErrorKind.Forbidden
        },
        {
            AcceptInvitationError.TenantNotFound,
            "tenant.accept_invitation_tenant_not_found",
            "The organization for this invitation no longer exists.",
            ErrorKind.NotFound
        },
        {
            AcceptInvitationError.AlreadyMember,
            "tenant.accept_invitation_already_member",
            "You are already a member of this organization.",
            ErrorKind.Conflict
        },
        {
            AcceptInvitationError.InvitationNotPending,
            "tenant.accept_invitation_not_pending",
            "This invitation is no longer pending.",
            ErrorKind.Conflict
        },
        {
            AcceptInvitationError.InvitationExpired,
            "tenant.accept_invitation_expired",
            "This invitation has expired.",
            ErrorKind.Invalid
        },
        {
            ChangeMemberRoleError.NotFound(Id),
            "tenant.change_role_member_not_found",
            $"User {Id} is not a member of this organization.",
            ErrorKind.NotFound
        },
        {
            ChangeMemberRoleError.LastOwner,
            "tenant.change_role_last_owner",
            "The last owner of an organization cannot be demoted.",
            ErrorKind.Conflict
        },
        {
            DeleteTenantError.NotFound(Id),
            "tenant.delete_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            InviteMemberError.TenantNotFound,
            "tenant.invite_tenant_not_found",
            "Your organization was not found.",
            ErrorKind.NotFound
        },
        {
            InviteMemberError.AlreadyMember,
            "tenant.invite_already_member",
            "This person is already a member of the organization.",
            ErrorKind.Conflict
        },
        {
            InviteMemberError.InvitationPending,
            "tenant.invite_already_pending",
            "An invitation for this email is already pending.",
            ErrorKind.Conflict
        },
        {
            RemoveMemberError.NotFound(Id),
            "tenant.remove_member_not_found",
            $"User {Id} is not a member of this organization.",
            ErrorKind.NotFound
        },
        {
            RemoveMemberError.LastOwner,
            "tenant.remove_member_last_owner",
            "The last owner of an organization cannot be removed.",
            ErrorKind.Conflict
        },
        {
            RevokeInvitationError.NotFound(Id),
            "tenant.revoke_invitation_not_found",
            $"Invitation {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            RevokeInvitationError.InvitationNotPending,
            "tenant.revoke_invitation_not_pending",
            "Only a pending invitation can be revoked.",
            ErrorKind.Conflict
        },
        {
            UpdateTenantError.NoActiveTenant,
            "tenant.update_forbidden",
            "No active organization was found for the current user.",
            ErrorKind.Forbidden
        },
        {
            UpdateTenantError.NotFound(Id),
            "tenant.update_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            VatCalculationError.NotFound(Id),
            "tenant.vat_tenant_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Definition_ErrorCase_ReturnsStableDefinition(
        IError error,
        string expectedCode,
        string expectedMessage,
        ErrorKind expectedKind)
    {
        var definition = error.Definition;

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(expectedKind, definition.Kind);
    }
}
