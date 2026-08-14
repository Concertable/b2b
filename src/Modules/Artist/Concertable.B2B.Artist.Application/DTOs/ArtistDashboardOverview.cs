using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.DTOs;

internal sealed record ArtistDashboardOverview(
    int ArtistId,
    string ArtistName,
    ProfileHealth ProfileHealth,
    StripeConnectStatus StripeConnect,
    ReviewSummary ReviewSummary);

internal sealed record ProfileHealth(int Completeness, IReadOnlyList<ProfileHealthItem> Items);

internal sealed record ProfileHealthItem(string Id, string Label, string Href, bool Done);

internal sealed record StripeConnectStatus(StripeConnectState State, string Href);

internal enum StripeConnectState
{
    Complete,
    Incomplete,
    ActionRequired,
    Pending
}
