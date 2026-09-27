using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record AssignConcertMemberError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(
            "Only a party to the concert can assign one of its own members to it."),
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        InvalidMembership => ErrorDefinition.Invalid<InvalidMembership>(
            "That membership is not a current member of the acting business."),
        AlreadyAssigned => ErrorDefinition.Conflict<AlreadyAssigned>(
            "That membership is already assigned to this concert."),
        NotAssigned => ErrorDefinition.NotFound<NotAssigned>(
            "That membership is not assigned to this concert."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this assignment was in flight.")
    };

    [ErrorCode("concert.member_assignment.not_permitted")]
    public partial record NotPermitted;

    [ErrorCode("concert.member_assignment.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.member_assignment.invalid_membership")]
    public partial record InvalidMembership;

    [ErrorCode("concert.member_assignment.already_assigned")]
    public partial record AlreadyAssigned;

    [ErrorCode("concert.member_assignment.not_assigned")]
    public partial record NotAssigned;

    [ErrorCode("concert.member_assignment.superseded")]
    public partial record Superseded(int ConcertId);
}
