import { useState } from "react";
import { Button } from "@concertable/web/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@concertable/web/components/ui/dialog";
import { Label } from "@concertable/web/components/ui/label";
import { Textarea } from "@concertable/web/components/ui/textarea";
import { Select } from "@concertable/web/components/Select";
import { useResolveReport } from "../hooks/useResolveReport";
import { REPORT_OUTCOME_LABELS, type ReportOutcome } from "../types";

interface OutcomeOption {
  value: ReportOutcome;
  label: string;
}

const outcomes: OutcomeOption[] = (
  Object.keys(REPORT_OUTCOME_LABELS) as ReportOutcome[]
).map((value) => ({ value, label: REPORT_OUTCOME_LABELS[value] }));

interface Props {
  reportId: number;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ResolveReportDialog({ reportId, open, onOpenChange }: Readonly<Props>) {
  const [outcome, setOutcome] = useState<OutcomeOption>();
  const [notes, setNotes] = useState("");
  const { parse, submit, isPending } = useResolveReport(reportId);

  const buffer = { outcome: outcome?.value, notes };
  const parsed = parse(buffer);

  const close = (next: boolean) => {
    if (isPending) return;
    onOpenChange(next);
  };

  const handleSubmit = () => {
    submit(buffer, () => {
      setOutcome(undefined);
      setNotes("");
      close(false);
    });
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent
        showCloseButton={!isPending}
        onEscapeKeyDown={(e) => isPending && e.preventDefault()}
        onInteractOutside={(e) => isPending && e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Resolve report</DialogTitle>
          <DialogDescription>
            Record the outcome of this report. This is visible to other admins.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2" data-testid="resolve-outcome">
            <Label>Outcome</Label>
            <Select
              options={outcomes}
              value={outcome}
              onChange={setOutcome}
              getLabel={(o) => o.label}
              getValue={(o) => o.value}
              placeholder="Choose an outcome"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="resolve-notes">Notes (optional)</Label>
            <Textarea
              id="resolve-notes"
              data-testid="resolve-notes"
              rows={4}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={() => close(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button
            data-testid="resolve-submit"
            disabled={isPending || !parsed.success}
            onClick={handleSubmit}
          >
            {isPending ? "Resolving..." : "Resolve"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
