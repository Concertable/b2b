using Concertable.B2B.Application.Domain.Entities;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Application.Application.Strategies;

internal interface INameCommitment : IDealStrategy
{
    PaymentOperationReference Name(ApplicationEntity application);
}
