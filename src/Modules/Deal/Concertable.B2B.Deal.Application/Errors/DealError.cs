using Concertable.Kernel.Errors;

namespace Concertable.B2B.Deal.Application.Errors;

internal sealed record DealError : IError
{
    private DealError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static DealError NotFound(int dealId) =>
        new(ErrorDefinition.NotFound(
            "deal.get.not_found",
            $"Deal {dealId} was not found."));
}
