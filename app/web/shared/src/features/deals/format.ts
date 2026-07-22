import type {
  Deal,
  DoorSplitDeal,
  FlatFeeDeal,
  VenueHireDeal,
  VersusDeal,
} from "./types";

const summaryRegistry: Record<Deal["$type"], (deal: Deal) => string> = {
  flatFee: (c) => `£${(c as FlatFeeDeal).fee}`,
  doorSplit: (c) => `${(c as DoorSplitDeal).artistDoorPercent}% door`,
  versus: (c) => {
    const v = c as VersusDeal;
    return `£${v.guarantee} vs ${v.artistDoorPercent}%`;
  },
  venueHire: (c) => `£${(c as VenueHireDeal).hireFee} hire fee`,
};

export function dealSummary(deal: Deal): string {
  return summaryRegistry[deal.$type](deal);
}
