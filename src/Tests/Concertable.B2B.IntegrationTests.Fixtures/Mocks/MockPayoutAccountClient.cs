using Concertable.Payment.Client;
using Concertable.Testing.Integration;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

/// <summary>Stands in for B2B's gRPC payout client so the Tenant StripeAccount proxy can be exercised without a
/// live Payment service. Records the owner id it was last called with, so a test can assert the proxy passes the
/// active TENANT (not the user).</summary>
public sealed class MockPayoutAccountClient : IPayoutAccountClient, IResettable
{
    public Guid? LastOwnerId { get; private set; }

    public void Reset() => LastOwnerId = null;

    public Task<string?> GetOnboardingLinkAsync(Guid ownerId, CancellationToken ct = default)
    {
        LastOwnerId = ownerId;
        return Task.FromResult<string?>("https://mock-stripe-onboarding.local");
    }

    public Task<PayoutAccountStatus> GetAccountStatusAsync(Guid ownerId, CancellationToken ct = default)
    {
        LastOwnerId = ownerId;
        return Task.FromResult(PayoutAccountStatus.Verified);
    }

    public Task<SavedCard?> GetPaymentMethodAsync(Guid ownerId, CancellationToken ct = default)
    {
        LastOwnerId = ownerId;
        return Task.FromResult<SavedCard?>(new SavedCard("visa", "4242", 12, 2030));
    }

    public Task<string?> CreateSetupIntentAsync(Guid ownerId, CancellationToken ct = default)
    {
        LastOwnerId = ownerId;
        return Task.FromResult<string?>("seti_mock_secret");
    }
}
