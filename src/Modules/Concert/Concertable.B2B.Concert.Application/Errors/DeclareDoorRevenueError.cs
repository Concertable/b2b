using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record DeclareDoorRevenueError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) =>
            ErrorDefinition.For<DeclareDoorRevenueError>().NotFound<ConcertNotFound>(
                $"Concert {concertId} was not found."),
        VenueForbidden =>
            ErrorDefinition.For<DeclareDoorRevenueError>().Forbidden<VenueForbidden>(
                "Only the concert's venue can declare its door revenue."),
        WrongDealType =>
            ErrorDefinition.For<DeclareDoorRevenueError>().Invalid<WrongDealType>(
                "Door revenue can only be declared for a revenue-share concert."),
        TooEarly =>
            ErrorDefinition.For<DeclareDoorRevenueError>().Invalid<TooEarly>(
                "Door revenue can only be declared after the concert has ended."),
        AlreadySettled =>
            ErrorDefinition.For<DeclareDoorRevenueError>().Conflict<AlreadySettled>(
                "Door revenue can only be declared before the concert has settled.")
    };

    [ErrorCode("concert.door_revenue.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.door_revenue.forbidden")]
    public partial record VenueForbidden;

    [ErrorCode("concert.door_revenue.wrong_deal_type")]
    public partial record WrongDealType;

    [ErrorCode("concert.door_revenue.too_early")]
    public partial record TooEarly;

    [ErrorCode("concert.door_revenue.already_settled")]
    public partial record AlreadySettled;
}
