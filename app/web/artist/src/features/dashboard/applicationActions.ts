import type { ActionLink } from "@concertable/shared/types/common";

export type ApplicationActionName = "checkout" | "withdraw";

export type ApplicationActions = {
  [K in ApplicationActionName]?: ActionLink;
};

export const APPLICATION_ACTION_LABELS:
  Record<ApplicationActionName, string> = {
  checkout: "Pay & confirm",
  withdraw: "Withdraw",
};
