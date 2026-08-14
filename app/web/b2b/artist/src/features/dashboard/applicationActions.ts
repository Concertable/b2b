import type { ActionLink } from "@concertable/shared/types/common";

export type ApplicationActionName = "withdraw" | "contract";

export type ApplicationActions = {
  [K in ApplicationActionName]?: ActionLink;
};

export const APPLICATION_ACTION_LABELS: Record<ApplicationActionName, string> =
  {
    withdraw: "Withdraw",
    contract: "Contract",
  };
