import type { ActionLink } from "@concertable/shared/types/common";

export type ApplicationActionName =
  "accept" | "checkout" | "decline" | "cancel" | "contract";

export type ApplicationActions = {
  [K in ApplicationActionName]?: ActionLink;
};

export const APPLICATION_ACTION_LABELS: Record<ApplicationActionName, string> =
  {
    accept: "Accept",
    checkout: "Continue",
    decline: "Decline",
    cancel: "Cancel",
    contract: "Contract",
  };
