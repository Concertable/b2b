import type { ComponentType } from "react";
import type {
  Deal,
  FlatFeeDeal,
  DoorSplitDeal,
  VersusDeal,
  VenueHireDeal,
} from "../types";

function FlatFeeDetails({ deal }: { deal: FlatFeeDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Flat Fee</p>
      <p className="text-muted-foreground mt-2 text-sm">Fee</p>
      <p className="font-medium">£{deal.fee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{deal.paymentMethod}</p>
    </div>
  );
}

function DoorSplitDetails({ deal }: { deal: DoorSplitDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Door Split</p>
      <p className="text-muted-foreground mt-2 text-sm">Artist Door %</p>
      <p className="font-medium">{deal.artistDoorPercent}%</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{deal.paymentMethod}</p>
    </div>
  );
}

function VersusDetails({ deal }: { deal: VersusDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Versus</p>
      <p className="text-muted-foreground mt-2 text-sm">Guarantee</p>
      <p className="font-medium">£{deal.guarantee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Artist Door %</p>
      <p className="font-medium">{deal.artistDoorPercent}%</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{deal.paymentMethod}</p>
    </div>
  );
}

function VenueHireDetails({ deal }: { deal: VenueHireDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Venue Hire</p>
      <p className="text-muted-foreground mt-2 text-sm">Hire Fee</p>
      <p className="font-medium">£{deal.hireFee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{deal.paymentMethod}</p>
    </div>
  );
}

const dealRegistry = {
  flatFee: FlatFeeDetails,
  doorSplit: DoorSplitDetails,
  versus: VersusDetails,
  venueHire: VenueHireDetails,
} as Record<Deal["$type"], ComponentType<{ deal: Deal }>>;

interface Props {
  deal: Deal;
}

export function DealDetails({ deal }: Readonly<Props>) {
  const Component = dealRegistry[deal.$type];
  return <Component deal={deal} />;
}
