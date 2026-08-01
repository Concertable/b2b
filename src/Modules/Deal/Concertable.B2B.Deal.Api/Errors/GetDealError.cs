using Concertable.Kernel.Errors;

namespace Concertable.B2B.Deal.Api.Errors;

internal sealed record GetDealError : IError
{
    private GetDealError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static GetDealError NotFound(int dealId) =>
        new(ErrorDefinition.NotFound(
            "deal.get.not_found",
            $"Deal {dealId} was not found."));
}
