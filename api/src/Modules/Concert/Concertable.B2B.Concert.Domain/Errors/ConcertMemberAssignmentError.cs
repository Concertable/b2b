using Dunet;

namespace Concertable.B2B.Concert.Domain.Errors;

[Union(EnableImplicitConversions = false)]
public abstract partial record ConcertMemberAssignmentError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(
            "Only a party to the concert can assign one of its own members to it."),
        AlreadyAssigned => ErrorDefinition.Conflict<AlreadyAssigned>(
            "That membership is already assigned to this concert.")
    };

    public partial record NotPermitted;
    public partial record AlreadyAssigned;
}
