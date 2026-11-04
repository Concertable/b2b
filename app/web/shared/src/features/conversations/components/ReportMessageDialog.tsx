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
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { useConversationMessagesQuery } from "../hooks/useConversationMessagesQuery";
import { useReportMessageMutation } from "../hooks/useReportMessageMutation";
import {
  reportCategories,
  reportCategoryLabels,
  reportMessageRequestSchema,
} from "../schemas/reportMessageRequestSchema";
import type {
  ReportMessageFormValues,
  ReportMessageRequest,
  SelectedMessage,
} from "../types";

type ReportCategory = (typeof reportCategories)[number];

const categoryOptions = reportCategories.map((value) => ({
  value,
  label: reportCategoryLabels[value],
}));

export function ReportMessageDialog({
  session,
  selected,
  onClose,
}: Readonly<{
  session: TenantSession;
  selected: SelectedMessage;
  onClose: () => void;
}>) {
  const messages = useConversationMessagesQuery(session, selected.conversationId);
  const message = messages.data?.find((item) => item.id === selected.messageId);
  const canReport = !messages.isError && message?.actions.report != null;
  const report = useReportMessageMutation(session, selected);
  const {
    control,
    register,
    handleSubmit,
    formState: { errors, isValid },
  } = useForm<ReportMessageFormValues, unknown, ReportMessageRequest>({
    resolver: zodResolver(reportMessageRequestSchema),
    defaultValues: { details: "" },
    mode: "onChange",
  });

  const close = () => {
    if (!report.isPending) onClose();
  };
  const submit = (request: ReportMessageRequest) => {
    if (canReport && !report.isPending && tenantSession.isCurrent(session))
      report.mutate(request);
  };

  return (
    <Dialog open onOpenChange={(next) => !next && close()}>
      <DialogContent
        showCloseButton={!report.isPending}
        onEscapeKeyDown={(event) => report.isPending && event.preventDefault()}
        onInteractOutside={(event) => report.isPending && event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Report this message</DialogTitle>
          <DialogDescription>
            Tell us what is wrong with this message. We review every report.
          </DialogDescription>
        </DialogHeader>

        {report.isSuccess ? (
          <p data-testid="report-confirmation" className="text-sm">
            Thanks — your report has been submitted.
          </p>
        ) : (
          <form
            id="report-message-form"
            onSubmit={handleSubmit(submit)}
            className="space-y-4"
          >
            <div className="space-y-2" data-testid="report-category">
              <Label>Reason</Label>
              <Controller
                control={control}
                name="category"
                render={({ field }) => (
                  <Select
                    options={categoryOptions}
                    value={categoryOptions.find(
                      (option) => option.value === field.value,
                    )}
                    onChange={(option) => field.onChange(option.value)}
                    getLabel={(option) => option.label}
                    getValue={(option): ReportCategory => option.value}
                    placeholder="Choose a reason"
                  />
                )}
              />
              {errors.category && (
                <p className="text-muted-foreground text-sm">
                  Choose a reason to submit your report.
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="report-details">Details (optional)</Label>
              <Textarea
                id="report-details"
                data-testid="report-details"
                aria-invalid={errors.details !== undefined}
                rows={4}
                {...register("details")}
              />
              {errors.details && (
                <p className="text-destructive text-sm">{errors.details.message}</p>
              )}
            </div>

            {!canReport && !messages.isPending && (
              <p role="alert" className="text-destructive text-sm">
                This message is no longer available to report.
              </p>
            )}
            {report.isError && (
              <p role="alert" className="text-destructive text-sm">
                We could not submit your report. Please try again.
              </p>
            )}
          </form>
        )}

        <DialogFooter>
          {report.isSuccess ? (
            <Button onClick={close}>Close</Button>
          ) : (
            <>
              <Button variant="ghost" onClick={close} disabled={report.isPending}>
                Cancel
              </Button>
              <Button
                type="submit"
                form="report-message-form"
                data-testid="report-submit"
                disabled={!isValid || !canReport || report.isPending}
              >
                {report.isPending ? "Submitting..." : "Submit report"}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
