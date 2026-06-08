import type { FixturePersona } from "@concertable/shared/features/dashboard";
import { artistEmpty } from "./empty";
import { artistMid } from "./mid";
import { artistThriving } from "./thriving";
import type { ArtistDashboardFixture } from "./types";

export const artistFixtures: Record<FixturePersona, ArtistDashboardFixture> = {
  empty: artistEmpty,
  mid: artistMid,
  thriving: artistThriving,
};

export type { ArtistDashboardFixture };
