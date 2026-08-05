import { useState } from "react";
import type { ReactNode } from "react";
import { Button } from "@concertable/web/shared/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@concertable/web/shared/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@concertable/web/shared/components/ui/select";
import { Checkbox } from "@concertable/web/shared/components/ui/checkbox";
import { Label } from "@concertable/web/shared/components/ui/label";
import { DateRangeField } from "@concertable/web/shared/components/datetime/DateRangeField";
import {
  DealDetails,
  DealFields,
  DealSummaryLabel,
  DEAL_TYPE_LABELS,
} from "@b2b/features/deals";
import { useGenresQuery } from "@concertable/web/shared/features/search/hooks/useGenreQuery";
import { X } from "lucide-react";
import dayjs from "dayjs";
import type { Opportunity, OpportunityDraft } from "../../types";
import type { Deal, PaymentMethod } from "@b2b/features/deals";
import type { Genre } from "@concertable/web/shared/types/common";
import { genreLabel } from "@concertable/web/shared/types/common";

interface OpportunityCardProps {
  opportunity: Opportunity;
  actions?: ReactNode;
}

export function OpportunityCard({ opportunity, actions }: Readonly<OpportunityCardProps>) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <div
        className="border-border bg-card space-y-3 rounded-xl border p-4"
        data-testid={`opportunity-${opportunity.id}`}
      >
        <OpportunityRead
          opportunity={opportunity}
          actions={
            <>
              <Button variant="outline" size="sm" onClick={() => setOpen(true)}>
                View Deal
              </Button>
              {actions}
            </>
          }
        />
      </div>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Deal Details</DialogTitle>
          </DialogHeader>
          <DealDetails deal={opportunity.deal} />
          <DialogFooter showCloseButton />
        </DialogContent>
      </Dialog>
    </>
  );
}

function OpportunityRead({ opportunity, actions }: { opportunity: OpportunityDraft; actions?: ReactNode }) {
  return (
    <>
      <div className="flex items-start justify-between gap-4">
        <div className="space-y-1">
          <p className="text-muted-foreground text-sm">
            {dayjs(opportunity.startDate).format("D MMM YYYY")} —{" "}
            {dayjs(opportunity.endDate).format("D MMM YYYY")}
          </p>
          <DealSummaryLabel deal={opportunity.deal} />
        </div>
        {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
      </div>

      {opportunity.genres.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {opportunity.genres.map((genre) => (
            <span
              key={genre}
              className="bg-muted text-muted-foreground rounded-full px-2.5 py-0.5 text-xs"
            >
              {genreLabel(genre)}
            </span>
          ))}
        </div>
      )}
    </>
  );
}

interface EditCallbacks {
  onRemove: () => void;
  onSetDates: (start: string, end: string) => void;
  onSetDealType: (type: Deal["$type"]) => void;
  onSetDeal: (deal: Deal) => void;
  onSetPaymentMethod: (method: PaymentMethod) => void;
  onToggleGenre: (genre: Genre) => void;
}

interface OpportunityEditCardProps extends EditCallbacks {
  opportunity: OpportunityDraft;
}

export function OpportunityEditCard({ opportunity, onRemove, onSetDates, onSetDealType, onSetDeal, onSetPaymentMethod, onToggleGenre }: Readonly<OpportunityEditCardProps>) {
  const { data: genres } = useGenresQuery();
  const deal = opportunity.deal;

  return (
    <div
      className="border-border bg-card space-y-4 rounded-xl border p-4"
      data-testid="opportunity-card-edit"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 space-y-3">
          <DateRangeField
            startDate={opportunity.startDate}
            endDate={opportunity.endDate}
            onChange={onSetDates}
          />

          <div>
            <Label className="text-muted-foreground text-xs">Deal type</Label>
            <Select
              value={deal.$type}
              onValueChange={(v) => onSetDealType(v as Deal["$type"])}
            >
              <SelectTrigger data-testid="opportunity-deal-type">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(Object.keys(DEAL_TYPE_LABELS) as Deal["$type"][]).map((type) => (
                  <SelectItem key={type} value={type}>
                    {DEAL_TYPE_LABELS[type]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <DealFields deal={deal} onChange={onSetDeal} />

          <div>
            <Label className="text-muted-foreground text-xs">Payment method</Label>
            <Select
              value={deal.paymentMethod}
              onValueChange={(v) => onSetPaymentMethod(v as PaymentMethod)}
            >
              <SelectTrigger data-testid="opportunity-payment-method">
                <SelectValue placeholder="Select" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Cash">Cash</SelectItem>
                <SelectItem value="Transfer">Transfer</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div>
            <Label className="text-muted-foreground text-xs">Genres</Label>
            <div className="flex flex-wrap gap-2 pt-1">
              {genres?.map((genre) => {
                const checked = opportunity.genres.includes(genre);
                return (
                  <label
                    key={genre}
                    className="bg-muted flex cursor-pointer items-center gap-1.5 rounded-full px-2.5 py-1 text-xs"
                    data-testid={`opportunity-genre-${genre.toLowerCase()}`}
                  >
                    <Checkbox
                      checked={checked}
                      onCheckedChange={() => onToggleGenre(genre)}
                    />
                    {genreLabel(genre)}
                  </label>
                );
              })}
            </div>
          </div>
        </div>

        <Button
          variant="ghost"
          size="icon"
          onClick={onRemove}
          aria-label="Remove opportunity"
          data-testid="opportunity-remove"
        >
          <X className="size-4" />
        </Button>
      </div>
    </div>
  );
}
