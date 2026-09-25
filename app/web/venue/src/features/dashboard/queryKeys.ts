import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";

export const venueDashboardKey = (...parts: ReadonlyArray<unknown>) =>
  currentPrivateQueryKey("dashboard", "venue", ...parts);
