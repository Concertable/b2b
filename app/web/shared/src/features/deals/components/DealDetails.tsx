import type { ComponentType } from "react";
import type {
  Deal,
  FlatFeeDeal,
  DoorSplitDeal,
  VersusDeal,
  VenueHireDeal,
} from "../types";

function FlatFeeDetails({ contract }: { contract: FlatFeeDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Flat Fee</p>
      <p className="text-muted-foreground mt-2 text-sm">Fee</p>
      <p className="font-medium">£{contract.fee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{contract.paymentMethod}</p>
    </div>
  );
}

function DoorSplitDetails({ contract }: { contract: DoorSplitDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Door Split</p>
      <p className="text-muted-foreground mt-2 text-sm">Artist Door %</p>
      <p className="font-medium">{contract.artistDoorPercent}%</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{contract.paymentMethod}</p>
    </div>
  );
}

function VersusDetails({ contract }: { contract: VersusDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Versus</p>
      <p className="text-muted-foreground mt-2 text-sm">Guarantee</p>
      <p className="font-medium">£{contract.guarantee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Artist Door %</p>
      <p className="font-medium">{contract.artistDoorPercent}%</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{contract.paymentMethod}</p>
    </div>
  );
}

function VenueHireDetails({ contract }: { contract: VenueHireDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Type</p>
      <p className="font-medium">Venue Hire</p>
      <p className="text-muted-foreground mt-2 text-sm">Hire Fee</p>
      <p className="font-medium">£{contract.hireFee}</p>
      <p className="text-muted-foreground mt-2 text-sm">Payment</p>
      <p className="font-medium">{contract.paymentMethod}</p>
    </div>
  );
}

const contractRegistry = {
  flatFee: FlatFeeDetails,
  doorSplit: DoorSplitDetails,
  versus: VersusDetails,
  venueHire: VenueHireDetails,
} as Record<Deal["$type"], ComponentType<{ contract: Deal }>>;

interface Props {
  contract: Deal;
}

export function DealDetails({ contract }: Readonly<Props>) {
  const Component = contractRegistry[contract.$type];
  return <Component contract={contract} />;
}
