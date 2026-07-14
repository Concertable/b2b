import {
  DEAL_TYPE_LABELS,
  dealSummary,
  type Deal,
} from "@concertable/shared/features/deals";

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
