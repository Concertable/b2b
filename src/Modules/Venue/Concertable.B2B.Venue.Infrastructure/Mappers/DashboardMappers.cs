using Concertable.B2B.Venue.Application.DTOs;
using Concertable.Payment.Client;
using PaymentPayoutAccountStatus = Concertable.Payment.Client.Enums.PayoutAccountStatus;

namespace Concertable.B2B.Venue.Infrastructure.Mappers;

internal static class DashboardMappers
{
    extension(VenueDetails venue)
    {
        public ProfileHealth ToProfileHealth(PaymentPayoutAccountStatus payoutStatus)
        {
            ProfileHealthItem[] items =
            [
                new("name", "Set venue name", "/_venue/my", !string.IsNullOrWhiteSpace(venue.Name)),
                new("bio", "Add an about section", "/_venue/my", !string.IsNullOrWhiteSpace(venue.About)),
                new("banner", "Upload a banner image", "/_venue/my", !string.IsNullOrWhiteSpace(venue.BannerUrl)),
                new("avatar", "Upload a profile image", "/_venue/my", !string.IsNullOrWhiteSpace(venue.Avatar)),
                new("stripe", "Connect Stripe payouts", "/_venue/settings/payment", payoutStatus == PaymentPayoutAccountStatus.Verified)
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
                "/_venue/settings/payment");
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
