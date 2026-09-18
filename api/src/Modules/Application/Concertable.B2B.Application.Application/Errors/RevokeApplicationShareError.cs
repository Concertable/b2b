using Dunet;
using Reunion.Errors;

namespace Concertable.B2B.Application.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record RevokeApplicationShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoActiveTenant => ErrorDefinition.Forbidden<NoActiveTenant>(
            "This call is not acting in a tenant."),
        ApplicationNotFound(var applicationId) => ErrorDefinition.NotFound<ApplicationNotFound>(
            $"Application {applicationId} was not found."),
        GrantNotFound => ErrorDefinition.NotFound<GrantNotFound>(
            "No such grant on this application."),
        NotAShare => ErrorDefinition.Invalid<NotAShare>(
            "A party's own access to its application is not a share, and is not revoked here."),
        NotTheIssuer => ErrorDefinition.Forbidden<NotTheIssuer>(
            "Only the tenant that issued a share can revoke it."),
        Superseded(var applicationId) => ErrorDefinition.Conflict<Superseded>(
            $"Application {applicationId} changed while this revocation was in flight.")
    };

    [ErrorCode("application.share.revoke.no_active_tenant")]
    public partial record NoActiveTenant;

    [ErrorCode("application.share.revoke.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("application.share.revoke.grant_not_found")]
    public partial record GrantNotFound;

    [ErrorCode("application.share.revoke.not_a_share")]
    public partial record NotAShare;

    [ErrorCode("application.share.revoke.not_the_issuer")]
    public partial record NotTheIssuer;

    [ErrorCode("application.share.revoke.superseded")]
    public partial record Superseded(int ApplicationId);
}
