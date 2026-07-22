import type { Deal, PaymentMethod } from "./types";

export const DEAL_TYPE_LABELS: Record<Deal["$type"], string> = {
  flatFee: "Flat Fee",
  doorSplit: "Door Split",
  versus: "Versus",
  venueHire: "Venue Hire",
};

export function defaultDeal(
  type: Deal["$type"],
  paymentMethod: PaymentMethod = "Transfer",
): Deal {
  switch (type) {
    case "flatFee":
      return { $type: "flatFee", paymentMethod, fee: 0 };
    case "doorSplit":
      return { $type: "doorSplit", paymentMethod, artistDoorPercent: 70 };
    case "versus":
      return {
        $type: "versus",
        paymentMethod,
        guarantee: 0,
        artistDoorPercent: 70,
      };
    case "venueHire":
      return { $type: "venueHire", paymentMethod, hireFee: 0 };
  }
}
