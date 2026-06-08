import type { PaymentAmount } from "@/features/concerts/types";
import type { SummaryLine } from "@/features/concerts/components/checkout/OrderSummaryCard";

export function summaryFor(amount: PaymentAmount): {
  lines: SummaryLine[];
  total: SummaryLine;
} {
  switch (amount.$type) {
    case "flat":
      return {
        lines: [{ label: "Fee", value: `£${amount.amount.toFixed(2)}` }],
        total: {
          label: "Total due now",
          value: `£${amount.amount.toFixed(2)}`,
        },
      };
    case "doorShare":
      return {
        lines: [
          { label: "Artist door share", value: `${amount.artistPercent}%` },
        ],
        total: {
          label: "Settled at concert end",
          value: `${amount.artistPercent}% of door`,
        },
      };
    case "guaranteedDoor":
      return {
        lines: [
          { label: "Guarantee", value: `£${amount.guarantee.toFixed(2)}` },
          { label: "Artist door share", value: `${amount.artistPercent}%` },
        ],
        total: {
          label: "Settled at concert end",
          value: `£${amount.guarantee.toFixed(2)} + ${amount.artistPercent}% door`,
        },
      };
  }
}
