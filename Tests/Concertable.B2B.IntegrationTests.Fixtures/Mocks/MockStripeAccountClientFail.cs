using Concertable.Payment.Application.DTOs;
using Concertable.Payment.Application.Interfaces;
using Concertable.Payment.Domain;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockStripeAccountClientFail : IStripeAccountClient
{
    public Task ProvisionCustomerAsync(Guid userId, string email, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task ProvisionConnectAccountAsync(Guid userId, string email, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<string> GetOnboardingLinkAsync(string stripeId) =>
        Task.FromResult("https://mock-stripe-onboarding.local");

    public Task<PayoutAccountStatus> GetAccountStatusAsync(string stripeId) =>
        Task.FromResult(PayoutAccountStatus.Verified);

    public Task<string> CreateSetupIntentAsync(string? stripeCustomerId) =>
        Task.FromResult("seti_mock_secret");

    public Task<PaymentMethodDto?> GetPaymentMethodDetailsAsync(string stripeCustomerId) =>
        Task.FromResult<PaymentMethodDto?>(new PaymentMethodDto("visa", "4242", 12, 2030));

    public Task<CheckoutSession> CreatePaymentSessionAsync(
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession("pi_mock_secret", "cuss_mock_secret", stripeCustomerId));

    public Task<CheckoutSession> CreateSetupSessionAsync(
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession("seti_mock_secret", "cuss_mock_secret", stripeCustomerId));

    public Task<CheckoutSession> CreateVerifySessionAsync(
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession("pi_mock_verify_secret", "cuss_mock_secret", stripeCustomerId));

    public Task<CheckoutSession> CreateHoldSessionAsync(
        string stripeCustomerId,
        decimal amount,
        IDictionary<string, string> metadata,
        CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession("pi_mock_hold_secret", "cuss_mock_secret", stripeCustomerId));
}
