import { NumberInput } from "@concertable/web/shared/components/ui/NumberInput";
import { Label } from "@concertable/web/shared/components/ui/label";
import type {
  Deal,
  FlatFeeDeal,
  DoorSplitDeal,
  VersusDeal,
  VenueHireDeal,
} from "../types";

interface FieldProps<T extends Deal> {
  deal: T;
  onChange: (next: T) => void;
}

function FlatFeeFields({ deal, onChange }: FieldProps<FlatFeeDeal>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Fee (£)</Label>
      <NumberInput
        min={0}
        step="0.01"
        value={deal.fee}
        onChange={(e) => onChange({ ...deal, fee: Number(e.target.value) })}
        data-testid="deal-flatfee-fee"
      />
    </div>
  );
}

function DoorSplitFields({
  deal,
  onChange,
}: FieldProps<DoorSplitDeal>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Artist door %</Label>
      <NumberInput
        min={0}
        max={100}
        value={deal.artistDoorPercent}
        onChange={(e) =>
          onChange({ ...deal, artistDoorPercent: Number(e.target.value) })
        }
        data-testid="deal-doorsplit-percent"
      />
    </div>
  );
}

function VersusFields({ deal, onChange }: FieldProps<VersusDeal>) {
  return (
    <div className="grid grid-cols-2 gap-3">
      <div>
        <Label className="text-muted-foreground text-xs">Guarantee (£)</Label>
        <NumberInput
          min={0}
          step="0.01"
          value={deal.guarantee}
          onChange={(e) =>
            onChange({ ...deal, guarantee: Number(e.target.value) })
          }
          data-testid="deal-versus-guarantee"
        />
      </div>
      <div>
        <Label className="text-muted-foreground text-xs">Artist door %</Label>
        <NumberInput
          min={0}
          max={100}
          value={deal.artistDoorPercent}
          onChange={(e) =>
            onChange({ ...deal, artistDoorPercent: Number(e.target.value) })
          }
          data-testid="deal-versus-percent"
        />
      </div>
    </div>
  );
}

function VenueHireFields({
  deal,
  onChange,
}: FieldProps<VenueHireDeal>) {
  return (
    <div>
      <Label className="text-muted-foreground text-xs">Hire fee (£)</Label>
      <NumberInput
        min={0}
        step="0.01"
        value={deal.hireFee}
        onChange={(e) =>
          onChange({ ...deal, hireFee: Number(e.target.value) })
        }
        data-testid="deal-venuehire-fee"
      />
    </div>
  );
}

interface Props {
  deal: Deal;
  onChange: (next: Deal) => void;
}

export function DealFields({ deal, onChange }: Readonly<Props>) {
  switch (deal.$type) {
    case "flatFee":
      return <FlatFeeFields deal={deal} onChange={onChange} />;
    case "doorSplit":
      return <DoorSplitFields deal={deal} onChange={onChange} />;
    case "versus":
      return <VersusFields deal={deal} onChange={onChange} />;
    case "venueHire":
      return <VenueHireFields deal={deal} onChange={onChange} />;
  }
}
