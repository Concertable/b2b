namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record DeclareDoorRevenueError : IError
{
    private DeclareDoorRevenueError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static DeclareDoorRevenueError NotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "concert.door_revenue.not_found",
            $"Concert {concertId} was not found."));

    internal static DeclareDoorRevenueError Forbidden() =>
        new(ErrorDefinition.Forbidden(
            "concert.door_revenue.forbidden",
            "Only the concert's venue can declare its door revenue."));

    internal static DeclareDoorRevenueError WrongDealType() =>
        new(ErrorDefinition.Invalid(
            "concert.door_revenue.wrong_deal_type",
            "Door revenue can only be declared for a revenue-share concert."));

    internal static DeclareDoorRevenueError TooEarly() =>
        new(ErrorDefinition.Invalid(
            "concert.door_revenue.too_early",
            "Door revenue can only be declared after the concert has ended."));

    internal static DeclareDoorRevenueError AlreadySettled() =>
        new(ErrorDefinition.Conflict(
            "concert.door_revenue.already_settled",
            "Door revenue can only be declared before the concert has settled."));
}
