using Concertable.B2B.Concert.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class SelfBillingAgreementResponseMapper
{
    private const string Path = "/api/self-billing-agreement";
    private static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);

    /// <summary>
    /// Turns the caller's latest agreement (regardless of expiry) into its self-service status + affordance:
    /// <c>grant</c> when none was ever held, <c>renew</c> once the held one has lapsed or is within the 30-day
    /// renewal window, and a PDF link only while one is in force. Grant/Renew are the same POST — the split
    /// carries the label the SPA renders.
    /// </summary>
    public static SelfBillingAgreementResponse ToResponse(this SelfBillingAgreementDto? latest, DateTime utcNow)
    {
        var post = new ActionLink(Path, HttpMethods.Post);

        if (latest is null)
            return new SelfBillingAgreementResponse
            {
                Status = SelfBillingAgreementStatus.None,
                Actions = new SelfBillingAgreementActions(Grant: post, Renew: null, Pdf: null),
            };

        var inForce = latest.ExpiresAtUtc > utcNow;
        var renew = !inForce || latest.ExpiresAtUtc - utcNow <= RenewalWindow ? post : null;

        return new SelfBillingAgreementResponse
        {
            Status = inForce ? SelfBillingAgreementStatus.Active : SelfBillingAgreementStatus.Expired,
            SupplierLegalName = latest.SupplierLegalName,
            AcceptedAtUtc = latest.AcceptedAtUtc,
            ExpiresAtUtc = latest.ExpiresAtUtc,
            Actions = new SelfBillingAgreementActions(
                Grant: null,
                Renew: renew,
                Pdf: inForce ? new ActionLink($"{Path}/pdf", HttpMethods.Get) : null),
        };
    }
}
