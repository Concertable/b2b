import { monthsAgoIso } from "@concertable/shared/features/dashboard";
import type { VenueDashboardFixture } from "./types";

export const venueEmpty: VenueDashboardFixture = {
  overview: {
    venueId: 1,
    venueName: "The New Stage",
    profileHealth: {
      completeness: 25,
      items: [
        { id: "name", label: "Set venue name", href: "/_venue/my", done: true },
        { id: "bio", label: "Add an about section", href: "/_venue/my", done: false },
        { id: "banner", label: "Upload a banner image", href: "/_venue/my", done: false },
        { id: "stripe", label: "Connect Stripe payouts", href: "/stripe-return", done: false },
      ],
    },
    stripeConnect: {
      state: "Incomplete",
      href: "/stripe-return",
    },
    reviewSummary: {
      totalReviews: 0,
      averageRating: undefined,
    },
  },
  kpis: {
    applicationsToReview: 0,
    openOpportunities: 0,
    upcomingConcerts: 0,
    mtdRevenueCents: 0,
  },
  applicationsToReview: [],
  inbox: [],
  upcomingConcerts: [],
  ticketRevenue: Array.from({ length: 6 }, (_, i) => ({
    month: monthsAgoIso(5 - i),
    grossCents: 0,
    netCents: 0,
    count: 0,
  })),
  openOpportunities: [],
  activity: [],
  settlements: [],
  recentReviews: [],
};
