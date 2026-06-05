import { NumberInput } from "@/components/ui/NumberInput";
import { Label } from "@/components/ui/label";
import type {
  Contract,
  FlatFeeContract,
  DoorSplitContract,
  VersusContract,
  VenueHireContract,
} from "../types";

interface FieldProps<T extends Contract> {
  contract: T;
  onChange: (next: T) => void;
}

function FlatFeeFields({ contract, onChange }: FieldProps<FlatFeeContract>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Fee (£)</Label>
      <NumberInput
        min={0}
        step="0.01"
        value={contract.fee}
        onChange={(e) => onChange({ ...contract, fee: Number(e.target.value) })}
        data-testid="contract-flatfee-fee"
      />
    </div>
  );
}

function DoorSplitFields({
  contract,
  onChange,
}: FieldProps<DoorSplitContract>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Artist door %</Label>
      <NumberInput
        min={0}
        max={100}
        value={contract.artistDoorPercent}
        onChange={(e) =>
          onChange({ ...contract, artistDoorPercent: Number(e.target.value) })
        }
        data-testid="contract-doorsplit-percent"
      />
    </div>
  );
}

function VersusFields({ contract, onChange }: FieldProps<VersusContract>) {
  return (
    <div className="grid grid-cols-2 gap-3">
      <div>
        <Label className="text-muted-foreground text-xs">Guarantee (£)</Label>
        <NumberInput
          min={0}
          step="0.01"
          value={contract.guarantee}
          onChange={(e) =>
            onChange({ ...contract, guarantee: Number(e.target.value) })
          }
          data-testid="contract-versus-guarantee"
        />
      </div>
      <div>
        <Label className="text-muted-foreground text-xs">Artist door %</Label>
        <NumberInput
          min={0}
          max={100}
          value={contract.artistDoorPercent}
          onChange={(e) =>
            onChange({ ...contract, artistDoorPercent: Number(e.target.value) })
          }
          data-testid="contract-versus-percent"
        />
      </div>
    </div>
  );
}

function VenueHireFields({
  contract,
  onChange,
}: FieldProps<VenueHireContract>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Hire fee (£)</Label>
      <NumberInput
        min={0}
        step="0.01"
        value={contract.hireFee}
        onChange={(e) =>
          onChange({ ...contract, hireFee: Number(e.target.value) })
        }
        data-testid="contract-venuehire-fee"
      />
    </div>
  );
}

interface Props {
  contract: Contract;
  onChange: (next: Contract) => void;
}

export function ContractFields({ contract, onChange }: Readonly<Props>) {
  switch (contract.$type) {
    case "flatFee":
      return <FlatFeeFields contract={contract} onChange={onChange} />;
    case "doorSplit":
      return <DoorSplitFields contract={contract} onChange={onChange} />;
    case "versus":
      return <VersusFields contract={contract} onChange={onChange} />;
    case "venueHire":
      return <VenueHireFields contract={contract} onChange={onChange} />;
  }
}
