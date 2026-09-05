using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface IMintCommitment : IDealStrategy
{
    PaymentOperationReference Mint(int applicationId, int opportunityId, Guid artistTenantId);
}
