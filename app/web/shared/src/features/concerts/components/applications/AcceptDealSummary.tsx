import type { ComponentType } from "react";
import type {
  Deal,
  FlatFeeDeal,
  DoorSplitDeal,
  VersusDeal,
  VenueHireDeal,
} from "@b2b/features/deals";
import { PAYMENT_METHOD_LABELS } from "@b2b/features/deals";

function FlatFeeSummary({ deal }: { deal: FlatFeeDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">
        You agree to pay the artist
      </p>
      <p className="text-2xl font-semibold">£{deal.fee}</p>
      <p className="text-muted-foreground text-sm">
        via {PAYMENT_METHOD_LABELS[deal.paymentMethod]}
      </p>
    </div>
  );
}

function DoorSplitSummary({ deal }: { deal: DoorSplitDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Artist receives</p>
      <p className="text-2xl font-semibold">
        {deal.artistDoorPercent}% of door revenue
      </p>
      <p className="text-muted-foreground text-sm">
        settled after the event via {PAYMENT_METHOD_LABELS[deal.paymentMethod]}
      </p>
    </div>
  );
}

function VersusSummary({ deal }: { deal: VersusDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">Artist guaranteed</p>
      <p className="text-2xl font-semibold">£{deal.guarantee}</p>
      <p className="text-muted-foreground text-sm">
        or {deal.artistDoorPercent}% of door — whichever is greater, settled
        via {PAYMENT_METHOD_LABELS[deal.paymentMethod]}
      </p>
    </div>
  );
}

function VenueHireSummary({ deal }: { deal: VenueHireDeal }) {
  return (
    <div className="space-y-1">
      <p className="text-muted-foreground text-sm">
        Artist pays you a hire fee of
      </p>
      <p className="text-2xl font-semibold">£{deal.hireFee}</p>
      <p className="text-muted-foreground text-sm">
        via {PAYMENT_METHOD_LABELS[deal.paymentMethod]}
      </p>
    </div>
  );
}

const summaryRegistry = {
  flatFee: FlatFeeSummary,
  doorSplit: DoorSplitSummary,
  versus: VersusSummary,
  venueHire: VenueHireSummary,
} as Record<Deal["$type"], ComponentType<{ deal: Deal }>>;

interface Props {
  deal: Deal;
}

export function AcceptDealSummary({ deal }: Readonly<Props>) {
  const Summary = summaryRegistry[deal.$type];
  return <Summary deal={deal} />;
}
