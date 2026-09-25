import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";

export const artistDashboardKey = (...parts: ReadonlyArray<unknown>) =>
  currentPrivateQueryKey("dashboard", "artist", ...parts);
