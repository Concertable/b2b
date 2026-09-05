using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public interface IMockSettlementClient : ISettlementOperationsClient, IPaymentReportingClient, IResettable
{
    List<(Guid PayerId, Guid PayeeId, decimal Amount, PaymentOperationReference PaymentMethod, int ConcertId, Guid OperationId)> Payments { get; }
}
