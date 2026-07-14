import {
  DEAL_TYPE_LABELS,
  dealSummary,
  type Deal,
} from "@concertable/shared/features/deals";

interface Props {
  contract: Deal;
}

export function DealSummaryLabel({ contract }: Readonly<Props>) {
  return (
    <p className="font-medium">
      {DEAL_TYPE_LABELS[contract.$type]}{" "}
      <span className="text-muted-foreground text-sm font-normal">
        · {dealSummary(contract)}
      </span>
    </p>
  );
}
