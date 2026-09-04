using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public interface IMockManagerPaymentClient : IManagerPaymentOperationsClient, IManagerPaymentReportingClient, IResettable
{
    List<(Guid PayerId, Guid PayeeId, decimal Amount, string PaymentMethodId, int BookingId, Guid OperationId)> Payments { get; }

    List<(Guid PayerId, Guid PayeeId, decimal Amount, PaymentOperationReference PaymentMethod, int BookingId, Guid OperationId)> ReferencedPayments { get; }
}
