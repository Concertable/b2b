using Concertable.Kernel.Errors;

namespace Concertable.B2B.Deal.Application.Errors;

internal sealed record DealError(ErrorDefinition Definition) : IError
{
    internal static DealError NotFound(int dealId) =>
        new(ErrorDefinition.NotFound(
            "deal.get.not_found",
            $"Deal {dealId} was not found."));
}
