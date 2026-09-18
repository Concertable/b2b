using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Api.Mappers;

internal static class ApplicationMappers
{
    extension(ApplicationDto dto)
    {
        public ApplicationResponse ToResponse(
            BookingSummary? booking,
            MembershipSnapshot? actor,
            IPermissionCatalog permissions)
        {
            var isPending = dto.State == ApplicationState.Applied;
            var canDecide = actor is { } member
                && member.TenantId == dto.VenueTenantId
                && permissions.Grants(member.Role, TenantPermission.ApplicationsDecide);
            var canSubmit = actor is { } submitter
                && submitter.TenantId == dto.ArtistTenantId
                && permissions.Grants(submitter.Role, TenantPermission.ApplicationsSubmit);
            var canReadTerms = actor is { } reader
                && (reader.TenantId == dto.VenueTenantId || reader.TenantId == dto.ArtistTenantId)
                && permissions.Grants(reader.Role, TenantPermission.TermsRead);
            var checkoutCapable = dto.Opportunity.Deal.DealType.RequiresAcceptCheckout();
            var status = booking?.Status switch
            {
                BookingStatus.AwaitingConfirmation or BookingStatus.ConfirmationFailed when checkoutCapable =>
                    ApplicationStatus.AwaitingPayment,
                BookingStatus.Confirmed or BookingStatus.CancellationPending or BookingStatus.CancellationFailed =>
                    ApplicationStatus.Confirmed,
                BookingStatus.Cancelled => ApplicationStatus.Cancelled,
                _ => dto.Status
            };

            return new ApplicationResponse(
                dto.Id,
                dto.Artist,
                new OpportunitySummaryResponse(
                    dto.Opportunity.Id,
                    dto.Opportunity.VenueId,
                    dto.Opportunity.VenueName,
                    dto.Opportunity.StartDate,
                    dto.Opportunity.EndDate,
                    dto.Opportunity.Genres.ToList(),
                    dto.Opportunity.Deal),
                status,
                new ApplicationActions(
                    Accept: canDecide && isPending
                        ? new ActionLink($"/api/application/{dto.Id}/accept", HttpMethods.Post)
                        : null,
                    Checkout: canDecide && isPending && checkoutCapable
                        ? new ActionLink($"/api/application/{dto.Id}/checkout", HttpMethods.Post)
                        : null,
                    Decline: canDecide && isPending
                        ? new ActionLink($"/api/application/{dto.Id}/reject", HttpMethods.Post)
                        : null,
                    Cancel: canDecide && isPending && booking is null
                        ? new ActionLink($"/api/application/{dto.Id}/cancel", HttpMethods.Post)
                        : null,
                    Withdraw: canSubmit && isPending
                        ? new ActionLink($"/api/application/{dto.Id}/withdraw", HttpMethods.Post)
                        : null,
                    Contract: canReadTerms && booking is not null
                        ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                        : null));
        }
    }
}
