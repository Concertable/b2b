import { monthsAgoIso } from "@concertable/shared/features/dashboard";
import type { ArtistDashboardFixture } from "./types";

export const artistEmpty: ArtistDashboardFixture = {
  overview: {
    artistId: 1,
    artistName: "Quiet Static",
    profileHealth: {
      completeness: 30,
      items: [
        { id: "name", label: "Set artist name", href: "/_artist/my", done: true },
        { id: "bio", label: "Add an about section", href: "/_artist/my", done: false },
        { id: "banner", label: "Upload a banner image", href: "/_artist/my", done: false },
        { id: "stripe", label: "Connect Stripe payouts", href: "/stripe-return", done: false },
      ],
    },
    stripeConnect: { state: "Incomplete", href: "/stripe-return" },
    reviewSummary: { totalReviews: 0, averageRating: undefined },
  },
  kpis: {
    pendingApplications: 0,
    acceptedAwaitingCheckout: 0,
    upcomingConcerts: 0,
    mtdPayoutsCents: 0,
  },
  applications: [],
  inbox: [],
  upcomingConcerts: [],
  payouts: Array.from({ length: 6 }, (_, i) => ({
    month: monthsAgoIso(5 - i),
    grossCents: 0,
    netCents: 0,
    count: 0,
  })),
  recommendedOpportunities: [],
  activity: [],
  recentReviews: [],
};
