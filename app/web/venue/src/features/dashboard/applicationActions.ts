import {
  applicationActionLabels,
  type ApplicationActionsOf,
} from "@concertable/web-b2b/features/concerts";

export const APPLICATION_ACTION_NAMES = [
  "accept",
  "checkout",
  "decline",
  "cancel",
  "contract",
] as const;

export type ApplicationActionName = (typeof APPLICATION_ACTION_NAMES)[number];

export type ApplicationActions = ApplicationActionsOf<ApplicationActionName>;

export const APPLICATION_ACTION_LABELS = applicationActionLabels(
  APPLICATION_ACTION_NAMES,
);

export const APPLICATION_ACTION_VARIANTS: Record<
  ApplicationActionName,
  "default" | "outline"
> = {
  accept: "default",
  checkout: "default",
  decline: "outline",
  cancel: "outline",
  contract: "outline",
};
