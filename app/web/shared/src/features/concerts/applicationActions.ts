import type { ApplicationActionName } from "./types";

const LABELS: Record<ApplicationActionName, string> = {
  accept: "Accept",
  checkout: "Continue",
  decline: "Decline",
  cancel: "Cancel",
  withdraw: "Withdraw",
  contract: "Contract",
};

export function applicationActionLabels<TName extends ApplicationActionName>(
  names: readonly TName[],
): Record<TName, string> {
  return Object.fromEntries(
    names.map((name) => [name, LABELS[name]]),
  ) as Record<TName, string>;
}
