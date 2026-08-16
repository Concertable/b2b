using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Deal.Contracts;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Concert.Api.Responses;

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
    IDeal Deal);

internal sealed record VenueApplicationActions(
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Accept,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Checkout,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Decline,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Cancel,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Contract);

internal sealed record ArtistApplicationActions(
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Withdraw,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ActionLink? Contract);
