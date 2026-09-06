using Concertable.B2B.Application.Domain.Entities;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface ICommitmentReferenceStep : IDealStep
{
    PaymentOperationReference Resolve(ApplicationEntity application);
}
