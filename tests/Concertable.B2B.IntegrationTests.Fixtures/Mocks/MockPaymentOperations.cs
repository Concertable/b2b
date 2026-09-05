using Concertable.Payment.Contracts;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

/// <summary>
/// The operations B2B opened directly against Payment, newest last. Payment settles an operation by
/// publishing an outcome for its reference, so the webhook simulator needs to know which one is current;
/// the escrow commands settle over the bus instead and are the transport's business.
/// </summary>
public sealed class MockPaymentOperations : IResettable
{
    private readonly List<MockPaymentOperation> operations = [];

    public void Record(PaymentOperationReference reference, Guid? operationId = null)
    {
        lock (operations)
            operations.Add(new MockPaymentOperation(reference, operationId));
    }

    public MockPaymentOperation? Latest
    {
        get
        {
            lock (operations)
                return operations.Count == 0 ? null : operations[^1];
        }
    }

    public void Reset()
    {
        lock (operations)
            operations.Clear();
    }
}

public sealed record MockPaymentOperation(PaymentOperationReference Reference, Guid? OperationId);
