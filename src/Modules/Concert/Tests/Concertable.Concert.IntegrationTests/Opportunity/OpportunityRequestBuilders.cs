using Concertable.Concert.Application.Requests;
using Concertable.Testing.Integration;
using Concertable.Shared;

namespace Concertable.Concert.IntegrationTests.Opportunity;

internal static class OpportunityRequestBuilders
{
    public static OpportunityRequest BuildRequest(IContract contract) =>
        new()
        {
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddHours(3),
            Genres = [Genre.Rock],
            Contract = contract
        };

    public static OpportunityRequest BuildDefaultRequest() =>
        BuildRequest(new FlatFeeContract { PaymentMethod = PaymentMethod.Cash, Fee = 500 });
}
