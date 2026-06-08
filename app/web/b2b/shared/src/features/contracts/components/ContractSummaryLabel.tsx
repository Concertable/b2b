import {
  CONTRACT_TYPE_LABELS,
  contractSummary,
  type Contract,
} from "@concertable/shared/features/contracts";

interface Props {
  contract: Contract;
}

export function ContractSummaryLabel({ contract }: Readonly<Props>) {
  return (
    <p className="font-medium">
      {CONTRACT_TYPE_LABELS[contract.$type]}{" "}
      <span className="text-muted-foreground text-sm font-normal">
        · {contractSummary(contract)}
      </span>
    </p>
  );
}
