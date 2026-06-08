import { daysAgo, daysAhead, hoursAgo, monthsAgoIso } from "@concertable/shared/features/dashboard";
import type { ArtistDashboardFixture } from "./types";

export const artistThriving: ArtistDashboardFixture = {
  overview: {
    artistId: 3,
    artistName: "Halberd",
    profileHealth: {
      completeness: 100,
      items: [
        { id: "name", label: "Set artist name", href: "/_artist/my", done: true },
        { id: "bio", label: "Add an about section", href: "/_artist/my", done: true },
        { id: "banner", label: "Upload a banner image", href: "/_artist/my", done: true },
        { id: "genres", label: "Set genres", href: "/_artist/my", done: true },
        { id: "stripe", label: "Connect Stripe payouts", href: "/stripe-return", done: true },
        { id: "photos", label: "Add 3 photos", href: "/_artist/my", done: true },
      ],
    },
    stripeConnect: { state: "Complete", href: "/stripe-return" },
    reviewSummary: { totalReviews: 142, averageRating: 4.9 },
  },
  kpis: {
    pendingApplications: 9,
    acceptedAwaitingCheckout: 2,
    upcomingConcerts: 11,
    mtdPayoutsCents: 728_500,
    mtdPayoutsDeltaPercent: 21,
  },
  applications: [
    { id: 401, status: "AwaitingPayment", opportunity: { id: 9101, venueId: 31, venueName: "The Hare & Hounds", startDate: daysAhead(6), endDate: daysAhead(6), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 800, artistDoorPercent: 70 } }, actions: { checkout: { href: "/api/Application/401/checkout", method: "POST" } } },
    { id: 402, status: "AwaitingPayment", opportunity: { id: 9401, venueId: 32, venueName: "Northgate Hall", startDate: daysAhead(14), endDate: daysAhead(14), genres: ["Indie"], contract: { $type: "flatFee", paymentMethod: "Transfer", fee: 1400 } }, actions: { checkout: { href: "/api/Application/402/checkout", method: "POST" } } },
    { id: 403, status: "Pending", opportunity: { id: 9402, venueId: 33, venueName: "The Black Box", startDate: daysAhead(25), endDate: daysAhead(25), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 600, artistDoorPercent: 80 } }, actions: { withdraw: { href: "/api/Application/403", method: "DELETE" } } },
    { id: 404, status: "Pending", opportunity: { id: 9403, venueId: 34, venueName: "Quayside", startDate: daysAhead(33), endDate: daysAhead(33), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 900, artistDoorPercent: 60 } }, actions: { withdraw: { href: "/api/Application/404", method: "DELETE" } } },
    { id: 405, status: "Confirmed", opportunity: { id: 9404, venueId: 35, venueName: "Riverside Tap", startDate: daysAhead(3), endDate: daysAhead(3), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 420, artistDoorPercent: 50 } }, actions: {} },
  ],
  inbox: [
    { id: 1201, otherPartyName: "The Hare & Hounds", preview: "Set list looks great — see you Friday.", at: hoursAgo(1), unread: true, href: "/_artist/messages/1201" },
    { id: 1202, otherPartyName: "Northgate Hall", preview: "Quick Q on backline.", at: hoursAgo(3), unread: true, href: "/_artist/messages/1202" },
    { id: 1203, otherPartyName: "The Black Box", preview: "We loved your stuff.", at: hoursAgo(8), unread: false, href: "/_artist/messages/1203" },
    { id: 1204, otherPartyName: "Quayside", preview: "Pencil us in?", at: daysAgo(1), unread: false, href: "/_artist/messages/1204" },
  ],
  upcomingConcerts: [
    { id: 8801, name: "Riverside set", startDate: daysAhead(3), endDate: daysAhead(3), counterpartyName: "Riverside Tap", ticketsSold: 38, ticketsTotal: 50, href: "/_artist/concerts/8801" },
    { id: 8802, name: "Hare & Hounds launch", startDate: daysAhead(6), endDate: daysAhead(6), counterpartyName: "The Hare & Hounds", ticketsSold: 182, ticketsTotal: 200, href: "/_artist/concerts/8802" },
    { id: 8803, name: "Northgate", startDate: daysAhead(14), endDate: daysAhead(14), counterpartyName: "Northgate Hall", ticketsSold: 96, ticketsTotal: 250, href: "/_artist/concerts/8803" },
    { id: 8804, name: "Black Box", startDate: daysAhead(25), endDate: daysAhead(25), counterpartyName: "The Black Box", ticketsSold: 42, ticketsTotal: 100, href: "/_artist/concerts/8804" },
    { id: 8805, name: "Quayside late", startDate: daysAhead(33), endDate: daysAhead(33), counterpartyName: "Quayside", ticketsSold: 8, ticketsTotal: 120, href: "/_artist/concerts/8805" },
  ],
  payouts: [
    { month: monthsAgoIso(5), grossCents: 182_000, netCents: 158_400, count: 4 },
    { month: monthsAgoIso(4), grossCents: 246_500, netCents: 214_500, count: 5 },
    { month: monthsAgoIso(3), grossCents: 312_000, netCents: 271_400, count: 6 },
    { month: monthsAgoIso(2), grossCents: 428_500, netCents: 372_800, count: 7 },
    { month: monthsAgoIso(1), grossCents: 612_000, netCents: 532_400, count: 9 },
    { month: monthsAgoIso(0), grossCents: 728_500, netCents: 633_800, count: 11 },
  ],
  recommendedOpportunities: [
    { id: 9501, venueId: 41, venueName: "The Pillar", county: "Manchester", town: "Manchester", startDate: daysAhead(40), endDate: daysAhead(40), genres: ["Rock"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 900, artistDoorPercent: 70 }, fitScore: 92, href: "/find/opportunity/9501" },
    { id: 9502, venueId: 42, venueName: "Iron Works", county: "West Yorkshire", town: "Leeds", startDate: daysAhead(48), endDate: daysAhead(48), genres: ["Rock", "Indie"], contract: { $type: "flatFee", paymentMethod: "Transfer", fee: 1200 }, fitScore: 89, href: "/find/opportunity/9502" },
    { id: 9503, venueId: 43, venueName: "Halls of Light", county: "Edinburgh", town: "Edinburgh", startDate: daysAhead(55), endDate: daysAhead(55), genres: ["Rock"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 1500, artistDoorPercent: 50 }, fitScore: 84, href: "/find/opportunity/9503" },
  ],
  activity: [
    { id: "art1", type: "ApplicationAccepted", at: hoursAgo(2), subject: "The Hare & Hounds accepted you — checkout to confirm", url: "/_artist/application/401" },
    { id: "art2", type: "ApplicationAccepted", at: hoursAgo(6), subject: "Northgate Hall accepted you — checkout to confirm", url: "/_artist/application/402" },
    { id: "art3", type: "TicketSold", at: hoursAgo(11), subject: "Ticket sold: \"Hare & Hounds launch\" (now 182/200)", url: "/_artist/concerts/8802" },
    { id: "art4", type: "ConcertSettled", at: hoursAgo(28), subject: "\"Late slot\" settled — £842 to you", url: "/_artist/concerts/8700" },
    { id: "art5", type: "ReviewReceived", at: daysAgo(1), subject: "The Black Box left a 5-star review", url: "/_artist/reviews" },
    { id: "art6", type: "MessageReceived", at: daysAgo(2), subject: "New message from Quayside", url: "/_artist/messages/1204" },
  ],
  recentReviews: [
    { id: 9601, reviewerName: "The Black Box", stars: 5, excerpt: "Sold the room out. Professional from load-in to load-out.", at: daysAgo(1), href: "/_artist/reviews/9601" },
    { id: 9602, reviewerName: "The Hare & Hounds", stars: 5, excerpt: "One of the best sets we've hosted this year.", at: daysAgo(6), href: "/_artist/reviews/9602" },
    { id: 9603, reviewerName: "Quayside", stars: 5, excerpt: "Crowd loved every minute. Tight, loud, on-time.", at: daysAgo(14), href: "/_artist/reviews/9603" },
    { id: 9604, reviewerName: "Riverside Tap", stars: 4, excerpt: "Solid show, encore went over time slightly.", at: daysAgo(22), href: "/_artist/reviews/9604" },
    { id: 9605, reviewerName: "Northgate Hall", stars: 5, excerpt: "Booked in a return on the spot. Don't sleep on them.", at: daysAgo(31), href: "/_artist/reviews/9605" },
  ],
};
