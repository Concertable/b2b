import type { ComponentType } from "react";
import type {
  Contract,
  FlatFeeContract,
  DoorSplitContract,
  VersusContract,
  VenueHireContract,
} from "@b2b/features/contracts";

function FlatFeeSummary({ contract }: { contract: FlatFeeContract }) {
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

function DoorSplitSummary({ contract }: { contract: DoorSplitContract }) {
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

function VersusSummary({ contract }: { contract: VersusContract }) {
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

function VenueHireSummary({ contract }: { contract: VenueHireContract }) {
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
} as Record<Contract["$type"], ComponentType<{ contract: Contract }>>;

interface Props {
  contract: Contract;
}

export function AcceptContractSummary({ contract }: Readonly<Props>) {
  const Summary = summaryRegistry[contract.$type];
  return <Summary contract={contract} />;
}
