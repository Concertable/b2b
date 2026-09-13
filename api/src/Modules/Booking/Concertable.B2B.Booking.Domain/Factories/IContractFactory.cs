using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Booking.Domain.Factories;

internal interface IContractFactory : IDealStrategy
{
    ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DateTime createdAtUtc);
}

internal interface IContractFactory<TTerms> : IContractFactory
    where TTerms : DealTerms
{
    ContractEntity Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        TTerms terms,
        DateTime createdAtUtc);
}
