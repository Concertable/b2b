using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class SelfBillingAgreementMappers
{
    private const string Path = "/api/self-billing-agreement";

    extension(SelfBillingAgreementStatusDto status)
    {
        public SelfBillingAgreementResponse ToResponse()
        {
            var post = new ActionLink(Path, HttpMethods.Post);
            var agreement = status.Agreement;

            if (agreement is null)
                return new SelfBillingAgreementResponse
                {
                    Status = SelfBillingAgreementStatus.None,
                    Actions = new SelfBillingAgreementActions(Grant: post, Renew: null, Pdf: null),
                };

            return new SelfBillingAgreementResponse
            {
                Status = status.IsInForce
                    ? SelfBillingAgreementStatus.Active
                    : SelfBillingAgreementStatus.Expired,
                SupplierLegalName = agreement.SupplierLegalName,
                AcceptedAtUtc = agreement.AcceptedAtUtc,
                ExpiresAtUtc = agreement.ExpiresAtUtc,
                Actions = new SelfBillingAgreementActions(
                    Grant: null,
                    Renew: status.CanRenew ? post : null,
                    Pdf: status.IsInForce
                        ? new ActionLink($"{Path}/pdf", HttpMethods.Get)
                        : null),
            };
        }
    }
}
