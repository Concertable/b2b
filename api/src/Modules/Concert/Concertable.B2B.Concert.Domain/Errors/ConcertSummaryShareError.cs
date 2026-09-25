using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ConcertSummaryShareError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(
            "Only a party to the concert can disclose its summary."),
        InvalidValidity => ErrorDefinition.Invalid<InvalidValidity>(
            "A share must remain valid past the moment it is issued."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "This issuer already discloses the summary to that audience.")
    };

    public partial record NotPermitted;
    public partial record InvalidValidity;
    public partial record AlreadyShared;
}
