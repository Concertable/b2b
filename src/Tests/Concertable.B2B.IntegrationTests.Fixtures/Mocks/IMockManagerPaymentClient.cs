using Concertable.Payment.Client;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public interface IMockManagerPaymentClient : IManagerPaymentClient, IResettable
{
    List<(Guid PayerId, Guid PayeeId, decimal Amount, string PaymentMethodId, int BookingId)> Payments { get; }
}
