import type { ActionLink } from "@concertable/shared/types/common";

export type ApplicationActionName = "accept" | "decline";

export type ApplicationActions = {
  [K in ApplicationActionName]?: ActionLink;
};

export const APPLICATION_ACTION_LABELS:
  Record<ApplicationActionName, string> = {
  accept: "Accept",
  decline: "Decline",
};
