using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record ApplicationSummaryDto(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    ArtistSummary Artist,
    OpportunitySummary Opportunity,
    ApplicationStatus Status,
    ApplicationState State);

internal sealed record OpportunitySummary(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres);

internal sealed record ApplicationProposalDto(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    ArtistSummary Artist,
    OpportunityProposal Opportunity,
    ApplicationStatus Status,
    ApplicationState State);

internal sealed record OpportunityProposal(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres,
    DealDto Deal);
