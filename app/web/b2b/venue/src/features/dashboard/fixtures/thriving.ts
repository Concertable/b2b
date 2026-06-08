import { daysAgo, daysAhead, hoursAgo, monthsAgoIso } from "@concertable/shared/features/dashboard";
import type { VenueDashboardFixture } from "./types";

export const venueThriving: VenueDashboardFixture = {
  overview: {
    venueId: 3,
    venueName: "The Hare & Hounds",
    profileHealth: {
      completeness: 100,
      items: [
        { id: "name", label: "Set venue name", href: "/_venue/my", done: true },
        { id: "bio", label: "Add an about section", href: "/_venue/my", done: true },
        { id: "banner", label: "Upload a banner image", href: "/_venue/my", done: true },
        { id: "genres", label: "Set genres", href: "/_venue/my", done: true },
        { id: "stripe", label: "Connect Stripe payouts", href: "/stripe-return", done: true },
        { id: "photos", label: "Add 3 photos", href: "/_venue/my", done: true },
      ],
    },
    stripeConnect: { state: "Complete", href: "/stripe-return" },
    reviewSummary: { totalReviews: 213, averageRating: 4.8 },
  },
  kpis: {
    applicationsToReview: 23,
    applicationsToReviewDelta: 7,
    openOpportunities: 11,
    upcomingConcerts: 14,
    mtdRevenueCents: 1_842_300,
    mtdRevenueDeltaPercent: 28,
  },
  applicationsToReview: [
    { id: 201, status: "Pending", artist: { id: 51, name: "The Pavement Choir", rating: 4.8, genres: ["Indie"] }, opportunity: { id: 9101, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(6), endDate: daysAhead(6), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 800, artistDoorPercent: 70 } }, actions: { accept: { href: "/api/Application/201/accept", method: "POST" }, decline: { href: "/api/Application/201/decline", method: "POST" } } },
    { id: 202, status: "Pending", artist: { id: 52, name: "Mountain Cinema", rating: 4.6, genres: ["Rock"] }, opportunity: { id: 9101, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(6), endDate: daysAhead(6), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 800, artistDoorPercent: 70 } }, actions: { accept: { href: "/api/Application/202/accept", method: "POST" }, decline: { href: "/api/Application/202/decline", method: "POST" } } },
    { id: 203, status: "Pending", artist: { id: 53, name: "Verity & The Wires", rating: 4.5, genres: ["Indie"] }, opportunity: { id: 9102, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(13), endDate: daysAhead(13), genres: ["Indie"], contract: { $type: "flatFee", paymentMethod: "Transfer", fee: 1200 } }, actions: { accept: { href: "/api/Application/203/accept", method: "POST" }, decline: { href: "/api/Application/203/decline", method: "POST" } } },
    { id: 204, status: "Pending", artist: { id: 54, name: "Halberd", rating: 4.9, genres: ["Rock"] }, opportunity: { id: 9103, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(20), endDate: daysAhead(20), genres: ["Rock"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 600, artistDoorPercent: 80 } }, actions: { accept: { href: "/api/Application/204/accept", method: "POST" }, decline: { href: "/api/Application/204/decline", method: "POST" } } },
    { id: 205, status: "Pending", artist: { id: 55, name: "Sundial Twin", rating: 4.7, genres: ["Electronic"] }, opportunity: { id: 9102, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(13), endDate: daysAhead(13), genres: ["Indie"], contract: { $type: "flatFee", paymentMethod: "Transfer", fee: 1200 } }, actions: { accept: { href: "/api/Application/205/accept", method: "POST" }, decline: { href: "/api/Application/205/decline", method: "POST" } } },
  ],
  inbox: [
    { id: 701, otherPartyName: "The Pavement Choir", preview: "Set list attached — confirm and we're in.", at: hoursAgo(1), unread: true, href: "/_venue/messages/701" },
    { id: 702, otherPartyName: "Halberd", preview: "Re: tech rider — yes to backline.", at: hoursAgo(4), unread: true, href: "/_venue/messages/702" },
    { id: 703, otherPartyName: "Sundial Twin", preview: "Looking forward to it!", at: hoursAgo(11), unread: true, href: "/_venue/messages/703" },
    { id: 704, otherPartyName: "Mountain Cinema", preview: "Quick Q on door split…", at: daysAgo(1), unread: false, href: "/_venue/messages/704" },
    { id: 705, otherPartyName: "Verity & The Wires", preview: "Got it, ta!", at: daysAgo(2), unread: false, href: "/_venue/messages/705" },
  ],
  upcomingConcerts: [
    { id: 8001, name: "Pavement Choir launch", startDate: daysAhead(6), endDate: daysAhead(6), counterpartyName: "The Pavement Choir", ticketsSold: 188, ticketsTotal: 200, href: "/_venue/concerts/8001" },
    { id: 8002, name: "Verity & The Wires", startDate: daysAhead(13), endDate: daysAhead(13), counterpartyName: "Verity & The Wires", ticketsSold: 132, ticketsTotal: 200, href: "/_venue/concerts/8002" },
    { id: 8003, name: "Halberd", startDate: daysAhead(20), endDate: daysAhead(20), counterpartyName: "Halberd", ticketsSold: 64, ticketsTotal: 150, href: "/_venue/concerts/8003" },
    { id: 8004, name: "Sundial Twin", startDate: daysAhead(27), endDate: daysAhead(27), counterpartyName: "Sundial Twin", ticketsSold: 12, ticketsTotal: 250, href: "/_venue/concerts/8004" },
    { id: 8005, name: "Folk gala", startDate: daysAhead(34), endDate: daysAhead(34), counterpartyName: "Anna Q. + 3 more", ticketsSold: 4, ticketsTotal: 200, href: "/_venue/concerts/8005" },
  ],
  ticketRevenue: [
    { month: monthsAgoIso(5), grossCents: 482_000, netCents: 410_000, count: 7 },
    { month: monthsAgoIso(4), grossCents: 612_500, netCents: 521_900, count: 9 },
    { month: monthsAgoIso(3), grossCents: 728_000, netCents: 619_800, count: 11 },
    { month: monthsAgoIso(2), grossCents: 855_500, netCents: 728_200, count: 12 },
    { month: monthsAgoIso(1), grossCents: 1_442_000, netCents: 1_226_700, count: 14 },
    { month: monthsAgoIso(0), grossCents: 1_842_300, netCents: 1_565_900, count: 14 },
  ],
  openOpportunities: [
    { opportunity: { id: 9101, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(6), endDate: daysAhead(6), genres: ["Indie"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 800, artistDoorPercent: 70 } }, applicationCount: 12, daysUntilDeadline: 2 },
    { opportunity: { id: 9102, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(13), endDate: daysAhead(13), genres: ["Indie"], contract: { $type: "flatFee", paymentMethod: "Transfer", fee: 1200 } }, applicationCount: 7, daysUntilDeadline: 6 },
    { opportunity: { id: 9103, venueId: 3, venueName: "The Hare & Hounds", startDate: daysAhead(20), endDate: daysAhead(20), genres: ["Rock"], contract: { $type: "versus", paymentMethod: "Transfer", guarantee: 600, artistDoorPercent: 80 } }, applicationCount: 4, daysUntilDeadline: 13 },
  ],
  activity: [
    { id: "v1", type: "ApplicationReceived", at: hoursAgo(1), subject: "Sundial Twin applied to your \"Verity & The Wires\" opportunity", url: "/_venue/applications/205" },
    { id: "v2", type: "TicketSold", at: hoursAgo(2), subject: "Ticket sold: \"Pavement Choir launch\" (now 188/200)", url: "/_venue/concerts/8001" },
    { id: "v3", type: "ReviewReceived", at: hoursAgo(5), subject: "Iona K. left a 5-star review", url: "/_venue/reviews" },
    { id: "v4", type: "ConcertSettled", at: hoursAgo(18), subject: "\"Late night session\" settled — £842 to The Foundry", url: "/_venue/concerts/7900" },
    { id: "v5", type: "TicketSold", at: hoursAgo(22), subject: "Ticket sold: \"Halberd\" (now 64/150)", url: "/_venue/concerts/8003" },
    { id: "v6", type: "ApplicationAccepted", at: daysAgo(1), subject: "You accepted Halberd for the 4 May slot", url: "/_venue/applications/200" },
    { id: "v7", type: "MessageReceived", at: daysAgo(1), subject: "New message from Mountain Cinema", url: "/_venue/messages/704" },
  ],
  settlements: [
    { id: 801, concertId: 7900, concertName: "Late night session", at: hoursAgo(18), amountCents: 84_200, counterpartyName: "The Foundry", direction: "Out" },
    { id: 802, concertId: 7901, concertName: "Pop-up matinée", at: daysAgo(3), amountCents: 56_400, counterpartyName: "Anna Q.", direction: "Out" },
    { id: 803, concertId: 7902, concertName: "Songwriters circle", at: daysAgo(8), amountCents: 31_200, counterpartyName: "Joe Reeves", direction: "Out" },
    { id: 804, concertId: 7903, concertName: "Live launch", at: daysAgo(12), amountCents: 142_700, counterpartyName: "Brass Republic", direction: "Out" },
    { id: 805, concertId: 7904, concertName: "Open mic", at: daysAgo(15), amountCents: 18_400, counterpartyName: "Multiple artists", direction: "Out" },
  ],
  recentReviews: [
    { id: 9101, reviewerName: "Iona K.", stars: 5, excerpt: "Best small venue in town — sound is immaculate.", at: hoursAgo(5), href: "/_venue/reviews/9101" },
    { id: 9102, reviewerName: "The Foundry", stars: 5, excerpt: "Crowd brought the energy and the team handled load-out like pros.", at: hoursAgo(18), href: "/_venue/reviews/9102" },
    { id: 9103, reviewerName: "Anna Q.", stars: 4, excerpt: "Lovely room, would have liked a slightly later curfew.", at: daysAgo(3), href: "/_venue/reviews/9103" },
    { id: 9104, reviewerName: "Brass Republic", stars: 5, excerpt: "House engineer is a magician. Already booked our return.", at: daysAgo(12), href: "/_venue/reviews/9104" },
    { id: 9105, reviewerName: "Joe Reeves", stars: 5, excerpt: "Run with so much care. Paid promptly too.", at: daysAgo(20), href: "/_venue/reviews/9105" },
  ],
};
