import type { FixturePersona } from "@concertable/shared/features/dashboard";
import { venueEmpty } from "./empty";
import { venueMid } from "./mid";
import { venueThriving } from "./thriving";
import type { VenueDashboardFixture } from "./types";

export const venueFixtures: Record<FixturePersona, VenueDashboardFixture> = {
  empty: venueEmpty,
  mid: venueMid,
  thriving: venueThriving,
};

export type { VenueDashboardFixture };
