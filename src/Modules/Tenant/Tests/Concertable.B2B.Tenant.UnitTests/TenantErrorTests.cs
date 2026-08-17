using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Contracts;
using Reunion.Errors;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantErrorTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new AcceptInvitationError.InvitationNotFound(Id),
            "tenant.accept_invitation_not_found",
            $"Invitation {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            new AcceptInvitationError.EmailMismatch(),
            "tenant.accept_invitation_email_mismatch",
            "This invitation was issued to a different email address.",
            ErrorKind.Forbidden
        },
        {
            new AcceptInvitationError.TenantNotFound(),
            "tenant.accept_invitation_tenant_not_found",
            "The organization for this invitation no longer exists.",
            ErrorKind.NotFound
        },
        {
            new AcceptInvitationError.AlreadyMember(),
            "tenant.accept_invitation_already_member",
            "You are already a member of this organization.",
            ErrorKind.Conflict
        },
        {
            new AcceptInvitationError.InvitationNotPending(),
            "tenant.accept_invitation_not_pending",
            "This invitation is no longer pending.",
            ErrorKind.Conflict
        },
        {
            new AcceptInvitationError.InvitationExpired(),
            "tenant.accept_invitation_expired",
            "This invitation has expired.",
            ErrorKind.Invalid
        },
        {
            new AcceptInvitationError.Unauthenticated(),
            "tenant.accept_invitation_unauthenticated",
            "No authenticated user was found.",
            ErrorKind.Forbidden
        },
        {
            new ChangeMemberRoleError.MemberNotFound(Id),
            "tenant.change_role_member_not_found",
            $"User {Id} is not a member of this organization.",
            ErrorKind.NotFound
        },
        {
            new ChangeMemberRoleError.LastOwner(),
            "tenant.change_role_last_owner",
            "The last owner of an organization cannot be demoted.",
            ErrorKind.Conflict
        },
        {
            new DeleteTenantError.TenantNotFound(Id),
            "tenant.delete_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            new InviteMemberError.TenantNotFound(),
            "tenant.invite_tenant_not_found",
            "Your organization was not found.",
            ErrorKind.NotFound
        },
        {
            new InviteMemberError.AlreadyMember(),
            "tenant.invite_already_member",
            "This person is already a member of the organization.",
            ErrorKind.Conflict
        },
        {
            new InviteMemberError.InvitationPending(),
            "tenant.invite_already_pending",
            "An invitation for this email is already pending.",
            ErrorKind.Conflict
        },
        {
            new InviteMemberError.Unauthenticated(),
            "tenant.invite_unauthenticated",
            "No authenticated user was found.",
            ErrorKind.Forbidden
        },
        {
            new RemoveMemberError.MemberNotFound(Id),
            "tenant.remove_member_not_found",
            $"User {Id} is not a member of this organization.",
            ErrorKind.NotFound
        },
        {
            new RemoveMemberError.LastOwner(),
            "tenant.remove_member_last_owner",
            "The last owner of an organization cannot be removed.",
            ErrorKind.Conflict
        },
        {
            new RevokeInvitationError.InvitationNotFound(Id),
            "tenant.revoke_invitation_not_found",
            $"Invitation {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            new RevokeInvitationError.InvitationNotPending(),
            "tenant.revoke_invitation_not_pending",
            "Only a pending invitation can be revoked.",
            ErrorKind.Conflict
        },
        {
            new UpdateTenantError.TenantNotFound(Id),
            "tenant.update_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        },
        {
            new VatCalculationError.TenantNotFound(Id),
            "tenant.vat_tenant_not_found",
            $"Organization {Id} was not found.",
            ErrorKind.NotFound
        }
    };

    private static ValidationErrors ValidationErrors =>
        new([new("LegalName", "LegalName is required.")]);

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

    [Fact]
    public void Definition_UpdateValidation_ReturnsStableStructuredDefinition()
    {
        var definition = Assert.IsType<ValidationError>(
            new UpdateTenantError.Invalid(ValidationErrors).Definition);

        Assert.Equal("update.tenant_invalid", definition.Code);
        Assert.Equal("The organization update is invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Equal(["LegalName is required."], definition.Errors.Errors["LegalName"]);
    }
}
