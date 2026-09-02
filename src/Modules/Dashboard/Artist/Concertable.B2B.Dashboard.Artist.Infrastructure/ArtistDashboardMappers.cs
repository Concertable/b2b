using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Dashboard.Artist.Application;
using Concertable.Payment.Client;
using PaymentPayoutAccountStatus = Concertable.Payment.Client.Enums.PayoutAccountStatus;

namespace Concertable.B2B.Dashboard.Artist.Infrastructure;

internal static class ArtistDashboardMappers
{
    extension(ArtistDto artist)
    {
        public ProfileHealth ToProfileHealth(PaymentPayoutAccountStatus payoutStatus)
        {
            ProfileHealthItem[] items =
            [
                new("name", "Set artist name", "/_artist/my", !string.IsNullOrWhiteSpace(artist.Name)),
                new("bio", "Add an about section", "/_artist/my", !string.IsNullOrWhiteSpace(artist.About)),
                new("banner", "Upload a banner image", "/_artist/my", !string.IsNullOrWhiteSpace(artist.BannerUrl)),
                new("avatar", "Upload a profile image", "/_artist/my", !string.IsNullOrWhiteSpace(artist.Avatar)),
                new("genres", "Set genres", "/_artist/my", artist.Genres.Count > 0),
                new("stripe", "Connect Stripe payouts", "/_artist/settings/payment", payoutStatus == PaymentPayoutAccountStatus.Verified)
            ];
            var completeness = items.Count(item => item.Done) * 100 / items.Length;
            return new ProfileHealth(completeness, items);
        }
    }

    extension(PaymentPayoutAccountStatus payoutStatus)
    {
        public StripeConnectStatus ToStripeConnectStatus() =>
            new(
                payoutStatus switch
                {
                    PaymentPayoutAccountStatus.Verified => StripeConnectState.Complete,
                    PaymentPayoutAccountStatus.Pending => StripeConnectState.Pending,
                    _ => StripeConnectState.Incomplete
                },
                "/_artist/settings/payment");
    }

    extension(IReadOnlyList<MonthlyPaymentPoint> points)
    {
        public IReadOnlyList<MonthlyRevenuePoint> ToMonthlyRevenuePoints(DateTime firstMonth)
        {
            var byMonth = points.ToDictionary(point => point.Month);
            return Enumerable.Range(0, 6)
                .Select(offset => DateOnly.FromDateTime(firstMonth.AddMonths(offset)))
                .Select(month => byMonth.TryGetValue(month, out var point)
                    ? new MonthlyRevenuePoint(month, point.Gross.ToMinorUnits(), point.Net.ToMinorUnits(), point.Count)
                    : new MonthlyRevenuePoint(month, 0, 0, 0))
                .ToArray();
        }
    }
}
