using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Payment.Client;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// B2B's payout proxy: authorizes payout operations here — where membership lives — then forwards them to the
/// tenancy-agnostic Payment service over gRPC, scoped to the caller's active tenant rather than an
/// <c>owner</c> claim. Rationale in <c>api/CLAUDE.md</c> ("Shared code is the intersection, never the union").
/// </summary>
[ApiController]
[Route("api/[controller]")]
[HasPermission(Permissions.PayoutsManage)]
internal sealed class StripeAccountController : ControllerBase
{
    private readonly IPayoutAccountClient payoutAccountClient;
    private readonly ITenantContext tenantContext;

    public StripeAccountController(IPayoutAccountClient payoutAccountClient, ITenantContext tenantContext)
    {
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
    }

    [HttpGet("onboarding-link")]
    public async Task<ActionResult<string>> GetOnboardingLink() =>
        await payoutAccountClient.GetOnboardingLinkAsync(tenantContext.GetTenantId()) is { } link
            ? Ok(link)
            : BadRequest("No Stripe connect account found.");

    [HttpGet("account-status")]
    public async Task<ActionResult<PayoutAccountStatus>> GetAccountStatus() =>
        Ok(await payoutAccountClient.GetAccountStatusAsync(tenantContext.GetTenantId()));

    [HttpGet("payment-method")]
    public async Task<ActionResult<SavedCard?>> GetPaymentMethod() =>
        Ok(await payoutAccountClient.GetPaymentMethodAsync(tenantContext.GetTenantId()));

    [HttpPost("setup-intent")]
    public async Task<ActionResult<string>> CreateSetupIntent() =>
        await payoutAccountClient.CreateSetupIntentAsync(tenantContext.GetTenantId()) is { } secret
            ? Ok(secret)
            : Unauthorized();
}
