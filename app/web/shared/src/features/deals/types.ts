export type PaymentMethod = "Cash" | "Transfer";

interface DealBase {
  id?: number;
  paymentMethod: PaymentMethod;
}

export interface FlatFeeDeal extends DealBase {
  $type: "flatFee";
  fee: number;
}

export interface DoorSplitDeal extends DealBase {
  $type: "doorSplit";
  artistDoorPercent: number;
}

export interface VersusDeal extends DealBase {
  $type: "versus";
  guarantee: number;
  artistDoorPercent: number;
}

export interface VenueHireDeal extends DealBase {
  $type: "venueHire";
  hireFee: number;
}

export type Deal =
  | FlatFeeDeal
  | DoorSplitDeal
  | VersusDeal
  | VenueHireDeal;
