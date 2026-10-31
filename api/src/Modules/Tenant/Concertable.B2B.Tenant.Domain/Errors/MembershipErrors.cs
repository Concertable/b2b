using Dunet;

namespace Concertable.B2B.Tenant.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record MembershipRoleError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NoRoles => ErrorDefinition.Invalid<NoRoles>("A membership needs at least one role.")
    };

    public partial record NoRoles;
}
