using Concertable.B2B.Application.Contracts;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IMintCommitment : IDealStrategy
{
    PaymentCommitment Mint(int applicationId, int opportunityId, Guid artistTenantId);
}
