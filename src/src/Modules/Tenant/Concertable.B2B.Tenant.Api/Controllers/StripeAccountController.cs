using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Payment.Client;
using Concertable.Payment.Client.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// B2B's payout proxy: authorizes payout operations here — where membership lives — then forwards them to the
/// tenancy-agnostic Payment service over gRPC, scoped to the caller's active tenant rather than an
/// <c>owner</c> claim. Rationale in <c>api/CLAUDE.md</c> ("Shared code is the intersection, never the union").
/// </summary>
[ApiController]
[Route("api/[controller]")]
[HasPermission(SharedPermissions.PayoutsManage)]
internal sealed class StripeAccountController : ControllerBase
{
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;

    public StripeAccountController(IPayoutAccountOperationsClient payoutAccountClient, ITenantContext tenantContext)
    {
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
    }

    [HttpGet("onboarding-link")]
    public async Task<ActionResult<string>> GetOnboardingLink()
    {
        var link = await payoutAccountClient.GetOnboardingLinkAsync(tenantContext.GetTenantId());
        return link.TryGetValue(out var value)
            ? Ok(value)
            : BadRequest("No Stripe connect account found.");
    }

    [HttpGet("account-status")]
    public async Task<ActionResult<PayoutAccountStatus>> GetAccountStatus() =>
        Ok(await payoutAccountClient.GetAccountStatusAsync(tenantContext.GetTenantId()));

    [HttpGet("payment-method")]
    public async Task<ActionResult<SavedCard?>> GetPaymentMethod()
    {
        var paymentMethod = await payoutAccountClient.GetPaymentMethodAsync(tenantContext.GetTenantId());
        return Ok(paymentMethod.TryGetValue(out var value) ? value : null);
    }

    [HttpPost("setup-intent")]
    public async Task<ActionResult<string>> CreateSetupIntent()
    {
        var secret = await payoutAccountClient.CreateSetupIntentAsync(tenantContext.GetTenantId());
        return secret.TryGetValue(out var value)
            ? Ok(value)
            : Unauthorized();
    }
}
