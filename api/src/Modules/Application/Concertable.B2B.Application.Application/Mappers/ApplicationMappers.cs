using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.Mappers;

internal static class ApplicationMappers
{
    extension(ApplicationEntity application)
    {
        public ApplicationSummary ToSummary(
            ArtistSummary artist, OpportunityDto opportunity, VenueProfile venue) => new(
                application.Id,
                application.VenueTenantId,
                application.ArtistTenantId,
                artist,
                new OpportunitySummary(
                    opportunity.Id,
                    opportunity.VenueId,
                    venue.Name,
                    opportunity.StartDate,
                    opportunity.EndDate,
                    opportunity.Genres),
                application.State.ToStatus(),
                application.State);

        public ApplicationProposal ToProposal(
            ArtistSummary artist, OpportunityDto opportunity, VenueProfile venue, DealDto deal) => new(
                application.Id,
                application.VenueTenantId,
                application.ArtistTenantId,
                artist,
                new OpportunityProposal(
                    opportunity.Id,
                    opportunity.VenueId,
                    venue.Name,
                    opportunity.StartDate,
                    opportunity.EndDate,
                    opportunity.Genres,
                    deal),
                application.State.ToStatus(),
                application.State);
    }

    extension(ApplicationState state)
    {
        public ApplicationStatus ToStatus() => state switch
        {
            ApplicationState.Applied => ApplicationStatus.Pending,
            ApplicationState.Rejected => ApplicationStatus.Rejected,
            ApplicationState.Withdrawn => ApplicationStatus.Withdrawn,
            ApplicationState.Accepted => ApplicationStatus.Accepted,
            ApplicationState.Cancelled => ApplicationStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
