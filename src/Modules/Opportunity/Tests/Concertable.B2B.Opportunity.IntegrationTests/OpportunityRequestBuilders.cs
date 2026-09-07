using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Opportunity.Application.Requests;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Opportunity.IntegrationTests;

internal static class OpportunityRequestBuilders
{
    public static OpportunityRequest BuildRequest(DealDto deal, DateTime now) =>
        new()
        {
            StartDate = now.AddMonths(1),
            EndDate = now.AddMonths(1).AddHours(3),
            Genres = [Genre.Rock],
            Deal = deal
        };

    public static OpportunityRequest BuildDefaultRequest(DateTime now) =>
        BuildRequest(new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 500 }, now);
}
