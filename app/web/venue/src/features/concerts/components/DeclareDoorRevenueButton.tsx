import { useState } from "react";
import { toast } from "sonner";
import type { Concert } from "@concertable/shared/features/concerts/types";
import { doorRevenueRequestSchema } from "@concertable/shared/features/concerts/schemas/doorRevenueRequestSchema";
import { Button } from "@/components/ui/button";
import { NumberInput } from "@/components/ui/NumberInput";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useDeclareDoorRevenue } from "../hooks/useDeclareDoorRevenue";

interface Props {
  concert: Concert;
}

export function DeclareDoorRevenueButton({ concert }: Readonly<Props>) {
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState("");
  const [touched, setTouched] = useState(false);
  const declare = useDeclareDoorRevenue(concert.id);

  const parsed = doorRevenueRequestSchema.safeParse({ doorRevenue: Number(value) });
  const error = touched && !parsed.success ? parsed.error.issues[0].message : null;

  const external = Number(value) || 0;
  const concertableSales = (concert.ticketsSold ?? 0) * concert.price;
  const total = concertableSales + external;

  function handleConfirm() {
    if (!parsed.success) {
      setTouched(true);
      return;
    }
    declare.mutate(parsed.data, {
      onSuccess: () => {
        toast.success("Door takings recorded. The artist's share will settle shortly.");
        setOpen(false);
        setValue("");
        setTouched(false);
      },
    });
  }

  return (
    <>
      <Button data-testid="declare-door-revenue" onClick={() => setOpen(true)}>
        Enter door takings
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Enter door takings to settle</DialogTitle>
            <DialogDescription>
              Enter the <strong>external</strong> door take only — tickets sold on your own
              site or other ticketers, plus cash on the door. Exclude tickets sold through
              Concertable; those are already counted. This is a declared, contractually-binding
              figure.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="door-take">External door take (£)</Label>
              <NumberInput
                id="door-take"
                min={0}
                step="0.01"
                value={value}
                onChange={(e) => setValue(e.target.value)}
                onBlur={() => setTouched(true)}
                aria-invalid={error != null}
                data-testid="door-revenue-input"
              />
              {error && (
                <p className="text-destructive text-xs" data-testid="door-revenue-error">
                  {error}
                </p>
              )}
            </div>
            <dl className="text-muted-foreground space-y-1 text-sm">
              <div className="flex justify-between">
                <dt>Concertable ticket sales</dt>
                <dd data-testid="door-revenue-concertable">£{concertableSales.toFixed(2)}</dd>
              </div>
              <div className="flex justify-between">
                <dt>Your declared external take</dt>
                <dd>£{external.toFixed(2)}</dd>
              </div>
              <div className="text-foreground flex justify-between font-medium">
                <dt>Total the artist's share applies to</dt>
                <dd data-testid="door-revenue-total">£{total.toFixed(2)}</dd>
              </div>
            </dl>
          </div>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setOpen(false)}
              disabled={declare.isPending}
            >
              Cancel
            </Button>
            <Button
              data-testid="declare-door-revenue-confirm"
              onClick={handleConfirm}
              disabled={declare.isPending}
            >
              {declare.isPending ? "Saving..." : "Record takings"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
