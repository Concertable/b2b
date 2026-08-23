import { useState } from "react";
import type { MyConcert } from "@concertable/web-b2b/features/concerts";
import { Button } from "@concertable/web/components/ui/button";
import { NumberInput } from "@concertable/web/components/ui/NumberInput";
import { Label } from "@concertable/web/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@concertable/web/components/ui/dialog";
import { useDeclareDoorRevenue } from "../hooks/useDeclareDoorRevenue";

interface Props {
  concert: MyConcert;
}

export function DeclareDoorRevenueButton({ concert }: Readonly<Props>) {
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState("");
  const [touched, setTouched] = useState(false);
  const { errorMessage, concertableSales, external, total, declare, isPending } =
    useDeclareDoorRevenue(concert, value);

  const error = touched ? errorMessage : undefined;

  function handleConfirm() {
    const parsed = declare(() => {
      setOpen(false);
      setValue("");
      setTouched(false);
    });
    if (!parsed.success) setTouched(true);
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
              disabled={isPending}
            >
              Cancel
            </Button>
            <Button
              data-testid="declare-door-revenue-confirm"
              onClick={handleConfirm}
              disabled={isPending}
            >
              {isPending ? "Saving..." : "Record takings"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
