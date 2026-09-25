using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ShareConcertSummaryError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotPermitted => ErrorDefinition.Forbidden<NotPermitted>(
            "Only a party to the concert can disclose its summary."),
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        InvalidRecipient => ErrorDefinition.Invalid<InvalidRecipient>(
            "The recipient business or membership does not exist."),
        InvalidValidity => ErrorDefinition.Invalid<InvalidValidity>(
            "A share must remain valid past the moment it is issued."),
        AlreadyShared => ErrorDefinition.Conflict<AlreadyShared>(
            "This business already discloses the summary to that audience."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this share was in flight."),
        RequestConflict => ErrorDefinition.Conflict<RequestConflict>(
            "That request identity was already used for a different share.")
    };

    [ErrorCode("concert.summary_share.not_permitted")]
    public partial record NotPermitted;

    [ErrorCode("concert.summary_share.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.summary_share.invalid_recipient")]
    public partial record InvalidRecipient;

    [ErrorCode("concert.summary_share.invalid_validity")]
    public partial record InvalidValidity;

    [ErrorCode("concert.summary_share.already_shared")]
    public partial record AlreadyShared;

    [ErrorCode("concert.summary_share.superseded")]
    public partial record Superseded(int ConcertId);

    [ErrorCode("concert.summary_share.request_conflict")]
    public partial record RequestConflict;
}
