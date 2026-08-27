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
import { useRejectVerification } from "../hooks/useRejectVerification";
import {
  rejectVerificationRequestSchema,
  type RejectVerificationFormValues,
  type RejectVerificationRequest,
} from "../schemas/rejectVerificationRequestSchema";

interface Props {
  tenantId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function RejectVerificationDialog({
  tenantId,
  open,
  onOpenChange,
}: Readonly<Props>) {
  const { submit, isPending } = useRejectVerification(tenantId);
  const {
    control,
    handleSubmit,
    reset,
    formState: { isValid },
  } = useForm<RejectVerificationFormValues, unknown, RejectVerificationRequest>({
    resolver: zodResolver(rejectVerificationRequestSchema),
    defaultValues: { reason: "" },
    mode: "onChange",
  });

  const close = (next: boolean) => {
    if (isPending) return;
    onOpenChange(next);
  };

  const onValid = (request: RejectVerificationRequest) => {
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
          <DialogTitle>Reject verification</DialogTitle>
          <DialogDescription>
            The organisation is emailed this reason and can re-submit new
            evidence.
          </DialogDescription>
        </DialogHeader>

        <form
          id="reject-verification-form"
          onSubmit={handleSubmit(onValid)}
          className="space-y-4"
        >
          <div className="space-y-2">
            <Label htmlFor="reject-reason">Reason</Label>
            <Controller
              control={control}
              name="reason"
              render={({ field }) => (
                <Textarea
                  id="reject-reason"
                  data-testid="reject-reason"
                  rows={4}
                  {...field}
                />
              )}
            />
          </div>
        </form>

        <DialogFooter>
          <Button
            variant="ghost"
            onClick={() => close(false)}
            disabled={isPending}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            form="reject-verification-form"
            variant="destructive"
            data-testid="reject-submit"
            disabled={isPending || !isValid}
          >
            {isPending ? "Rejecting..." : "Reject"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
