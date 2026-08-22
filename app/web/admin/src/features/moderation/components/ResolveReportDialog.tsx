import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
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
import {
  resolveReportRequestSchema,
  type ResolveReportFormValues,
  type ResolveReportRequest,
} from "../schemas/resolveReportRequestSchema";
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
  const { submit, isPending } = useResolveReport(reportId);
  const {
    control,
    handleSubmit,
    reset,
    formState: { isValid },
  } = useForm<ResolveReportFormValues, unknown, ResolveReportRequest>({
    resolver: zodResolver(resolveReportRequestSchema),
    defaultValues: { notes: "" },
    mode: "onChange",
  });

  const close = (next: boolean) => {
    if (isPending) return;
    onOpenChange(next);
  };

  const onValid = (request: ResolveReportRequest) => {
    submit(request, () => {
      reset();
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

        <form
          id="resolve-report-form"
          onSubmit={handleSubmit(onValid)}
          className="space-y-4"
        >
          <div className="space-y-2" data-testid="resolve-outcome">
            <Label>Outcome</Label>
            <Controller
              control={control}
              name="outcome"
              render={({ field }) => (
                <Select
                  options={outcomes}
                  value={outcomes.find((o) => o.value === field.value)}
                  onChange={(o) => field.onChange(o.value)}
                  getLabel={(o) => o.label}
                  getValue={(o) => o.value}
                  placeholder="Choose an outcome"
                />
              )}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="resolve-notes">Notes (optional)</Label>
            <Controller
              control={control}
              name="notes"
              render={({ field }) => (
                <Textarea
                  id="resolve-notes"
                  data-testid="resolve-notes"
                  rows={4}
                  {...field}
                />
              )}
            />
          </div>
        </form>

        <DialogFooter>
          <Button variant="ghost" onClick={() => close(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button
            type="submit"
            form="resolve-report-form"
            data-testid="resolve-submit"
            disabled={isPending || !isValid}
          >
            {isPending ? "Resolving..." : "Resolve"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
