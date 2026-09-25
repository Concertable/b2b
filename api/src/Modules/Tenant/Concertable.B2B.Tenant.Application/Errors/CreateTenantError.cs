using Dunet;

namespace Concertable.B2B.Tenant.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CreateTenantError : IError
{
    public ErrorDefinition Definition => this switch
    {
        AlreadyOwnsTenant =>
            ErrorDefinition.Conflict<AlreadyOwnsTenant>("You have already created an organization."),
        Unauthenticated =>
            ErrorDefinition.Forbidden<Unauthenticated>("No authenticated user was found.")
    };

    [ErrorCode("tenant.create_already_exists")]
    public partial record AlreadyOwnsTenant;

    [ErrorCode("tenant.create_unauthenticated")]
    public partial record Unauthenticated;
}
