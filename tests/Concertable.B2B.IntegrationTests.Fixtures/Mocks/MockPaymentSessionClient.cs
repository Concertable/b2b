using Reunion;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

/// <summary>Stands in for Payment's session operations, reserving one operation id per reference the way
/// Payment's own reservation does.</summary>
public sealed class MockPaymentSessionClient : IPaymentSessionOperationsClient, IResettable
{
    private readonly Dictionary<PaymentOperationReference, Guid> operations = [];
    private readonly MockPaymentOperations paymentOperations;

    public List<(PaymentOperationReference Reference, PaymentSessionKind Kind, Guid PayerOwnerId)> Sessions { get; } = [];

    public MockPaymentSessionClient(MockPaymentOperations paymentOperations)
    {
        this.paymentOperations = paymentOperations;
    }

    public void Reset()
    {
        Sessions.Clear();
        operations.Clear();
    }

    public Task<Result<PaymentMethodSetup, PaymentOperationError>> SetupPaymentMethodAsync(
        PaymentMethodSetupRequest request,
        CancellationToken ct = default)
    {
        Record(request.Reference, request.Kind, request.PayerOwnerId);
        return Task.FromResult(
            Result<PaymentMethodSetup, PaymentOperationError>.Success(
                new PaymentMethodSetup(Secret(request.Reference), "cuss_mock_secret", "cus_mock")));
    }

    public Task<UnitResult<PaymentOperationError>> ValidatePaymentMethodAsync(
        PaymentMethodValidationRequest request,
        CancellationToken ct = default) =>
        Task.FromResult(
            operations.ContainsKey(request.Reference)
                ? UnitResult<PaymentOperationError>.Success()
                : UnitResult<PaymentOperationError>.Failure(new PaymentOperationError.PaymentMethodRequired()));

    public Task<Result<PaymentSessionDescriptor, PaymentOperationError>> CreateAsync(
        PaymentSessionOperationRequest request,
        CancellationToken ct = default)
    {
        var operationId = Record(request.Reference, request.Kind, request.PayerOwnerId);
        return Task.FromResult(
            Result<PaymentSessionDescriptor, PaymentOperationError>.Success(
                new PaymentSessionDescriptor(
                    new PaymentOperationIdentity(operationId, Guid.NewGuid(), 1),
                    request.Kind,
                    Secret(request.Reference),
                    "cuss_mock_secret",
                    "cus_mock")));
    }

    public Task<Result<PaymentSessionDescriptor, PaymentOperationError>> RetryAsync(
        PaymentSessionRetryRequest request,
        CancellationToken ct = default) =>
        Task.FromResult(
            Result<PaymentSessionDescriptor, PaymentOperationError>.Failure(
                new PaymentOperationError.Unknown()));

    public Task<Result<PaymentOperationSnapshot, PaymentOperationError>> GetStatusAsync(
        PaymentSessionStatusRequest request,
        CancellationToken ct = default) =>
        Task.FromResult(
            Result<PaymentOperationSnapshot, PaymentOperationError>.Success(
                new PaymentOperationSnapshot(
                    new PaymentOperationIdentity(request.OperationId, Guid.NewGuid(), 1),
                    PaymentOperationState.Succeeded,
                    PaymentOperationTerminalDisposition.OperationTerminal,
                    PaymentOperationRetryDisposition.NotRetryable,
                    null,
                    null,
                    null)));

    private Guid Record(PaymentOperationReference reference, PaymentSessionKind kind, Guid payerOwnerId)
    {
        lock (operations)
        {
            if (!operations.TryGetValue(reference, out var operationId))
            {
                operationId = Guid.CreateVersion7();
                operations.Add(reference, operationId);
            }

            Sessions.Add((reference, kind, payerOwnerId));
            paymentOperations.Record(reference);
            return operationId;
        }
    }

    private static string Secret(PaymentOperationReference reference) =>
        $"{reference.OperationType}:{reference.ClientReference}_secret";
}
