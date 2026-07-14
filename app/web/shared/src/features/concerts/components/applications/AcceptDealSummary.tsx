import type { ComponentType } from "react";
import type {
  Deal,
  FlatFeeDeal,
  DoorSplitDeal,
  VersusDeal,
  VenueHireDeal,
} from "@b2b/features/deals";

function FlatFeeSummary({ contract }: { contract: FlatFeeDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">
        You agree to pay the artist
      </p>
      <p className="text-2xl font-semibold">£{contract.fee}</p>
      <p className="text-muted-foreground text-sm">
        via {contract.paymentMethod}
      </p>
    </div>
  );
}

function DoorSplitSummary({ contract }: { contract: DoorSplitDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Artist receives</p>
      <p className="text-2xl font-semibold">
        {contract.artistDoorPercent}% of door revenue
      </p>
      <p className="text-muted-foreground text-sm">
        settled after the event via {contract.paymentMethod}
      </p>
    </div>
  );
}

function VersusSummary({ contract }: { contract: VersusDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Artist guaranteed</p>
      <p className="text-2xl font-semibold">£{contract.guarantee}</p>
      <p className="text-muted-foreground text-sm">
        or {contract.artistDoorPercent}% of door — whichever is greater, settled
        via {contract.paymentMethod}
      </p>
    </div>
  );
}

function VenueHireSummary({ contract }: { contract: VenueHireDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">
        Artist pays you a hire fee of
      </p>
      <p className="text-2xl font-semibold">£{contract.hireFee}</p>
      <p className="text-muted-foreground text-sm">
        via {contract.paymentMethod}
      </p>
    </div>
  );
}

const summaryRegistry = {
  flatFee: FlatFeeSummary,
  doorSplit: DoorSplitSummary,
  versus: VersusSummary,
  venueHire: VenueHireSummary,
} as Record<Deal["$type"], ComponentType<{ contract: Deal }>>;

interface Props {
  contract: Deal;
}

export function AcceptDealSummary({ contract }: Readonly<Props>) {
  const Summary = summaryRegistry[contract.$type];
  return <Summary contract={contract} />;
}
