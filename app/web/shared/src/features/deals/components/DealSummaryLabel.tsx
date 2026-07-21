import { DEAL_TYPE_LABELS } from "../defaults";
import { dealSummary } from "../format";
import type { Deal } from "../types";

interface Props {
  deal: Deal;
}

export function DealSummaryLabel({ deal }: Readonly<Props>) {
  return (
    <p className="font-medium">
      {DEAL_TYPE_LABELS[deal.$type]}{" "}
      <span className="text-muted-foreground text-sm font-normal">
        · {dealSummary(deal)}
      </span>
    </p>
  );
}
