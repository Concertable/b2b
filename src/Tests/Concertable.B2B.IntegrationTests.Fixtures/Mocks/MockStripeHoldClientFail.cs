using Concertable.Payment.Application.Interfaces;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockStripeHoldClientFail : IStripeHoldClient
{
    public Task<string> FindHeldIntentAsync(string stripeCustomerId, int applicationId, CancellationToken ct = default) =>
        throw new NotFoundException("No held payment intent found");

    public Task CancelAsync(string intentId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task CaptureAsync(string intentId, IDictionary<string, string> metadata, CancellationToken ct = default) =>
        Task.CompletedTask;
}
