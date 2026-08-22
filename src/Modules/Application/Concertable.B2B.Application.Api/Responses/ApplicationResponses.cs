using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Deal.Contracts;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Application.Api.Responses;

[JsonDerivedType(typeof(ApplicationResponse<VenueApplicationActions>))]
[JsonDerivedType(typeof(ApplicationResponse<ArtistApplicationActions>))]
internal record ApplicationResponse(
    int Id,
    ArtistSummary Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status);

internal sealed record ApplicationResponse<TActions>(
    int Id,
    ArtistSummary Artist,
    OpportunitySummaryResponse Opportunity,
    ApplicationStatus Status,
    TActions Actions)
    : ApplicationResponse(Id, Artist, Opportunity, Status);

internal sealed record OpportunitySummaryResponse(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    DealDto Deal);

internal sealed record VenueApplicationActions(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Accept,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Checkout,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Decline,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Cancel,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Contract);

internal sealed record ArtistApplicationActions(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Withdraw,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ActionLink? Contract);
