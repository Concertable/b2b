using Dunet;

namespace Concertable.B2B.Tenant.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record InvitationAcceptanceError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPending => ErrorDefinition.Conflict<NotPending>("This invitation is no longer pending."),
        Expired => ErrorDefinition.Invalid<Expired>("This invitation has expired.")
    };

    public partial record NotPending;

    public partial record Expired;
}

[Union(EnableImplicitConversions = false)]
public abstract partial record InvitationRevocationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPending => ErrorDefinition.Conflict<NotPending>("Only a pending invitation can be revoked.")
    };

    public partial record NotPending;
}
