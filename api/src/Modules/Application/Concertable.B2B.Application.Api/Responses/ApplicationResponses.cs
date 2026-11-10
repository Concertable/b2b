using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Deal.Contracts;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Application.Api.Responses;

internal sealed record ApplicationSummaryResponse(
    int Id,
    ArtistSummary Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status);

internal sealed record OpportunitySummaryResponse(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres);

internal sealed record ApplicationProposalResponse(
    int Id,
    ArtistSummary Artist,
    OpportunityProposalResponse Opportunity,
    ApplicationStatus Status,
    ApplicationActions Actions);

internal sealed record OpportunityProposalResponse(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    DealDto Deal);

internal sealed record ApplicationActions(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Accept,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Checkout,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Decline,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Cancel,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Withdraw,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Contract);
