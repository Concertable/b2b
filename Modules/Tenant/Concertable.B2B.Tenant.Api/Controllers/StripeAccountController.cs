using Concertable.B2B.Tenant.Api.Authorization;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Payment.Client;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

// B2B's payout proxy. Fronts the four Stripe-account operations for managers, resolving the owner as the
// ACTIVE TENANT (not the user) and calling Payment over gRPC — so authorization happens here, where the
// membership data lives, and Payment stays tenancy-agnostic. Replaces managers' direct calls to Payment's
// HTTP StripeAccountController; the route mirrors those endpoints so the SPAs only swap their base URL.
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
        await payoutAccountClient.GetOnboardingLinkAsync(Tenant) is { } link
            ? Ok(link)
            : BadRequest("No Stripe connect account found.");

    [HttpGet("account-status")]
    public async Task<ActionResult<PayoutAccountStatus>> GetAccountStatus() =>
        Ok(await payoutAccountClient.GetAccountStatusAsync(Tenant));

    [HttpGet("payment-method")]
    public async Task<ActionResult<SavedCard?>> GetPaymentMethod() =>
        Ok(await payoutAccountClient.GetPaymentMethodAsync(Tenant));

    [HttpPost("setup-intent")]
    public async Task<ActionResult<string>> CreateSetupIntent() =>
        await payoutAccountClient.CreateSetupIntentAsync(Tenant) is { } secret
            ? Ok(secret)
            : Unauthorized();

    // [HasPermission] already guarantees a resolved active tenant; the throw is a fail-closed backstop.
    private Guid Tenant => tenantContext.TenantId
        ?? throw new ForbiddenException("No active tenant for the current user.");
}
