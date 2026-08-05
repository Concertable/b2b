import { useState } from "react";
import { toast } from "sonner";
import { useCancelConcertMutation } from "@concertable/shared/features/concerts/hooks/useCancelConcertMutation";
import { Button } from "@concertable/web/shared/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@concertable/web/shared/components/ui/dialog";

interface Props {
  concertId: number;
}

export function CancelBookingButton({ concertId }: Readonly<Props>) {
  const [open, setOpen] = useState(false);
  const cancel = useCancelConcertMutation(concertId);

  function handleConfirm() {
    cancel.mutate(undefined, {
      onSuccess: () => {
        toast.success("Booking cancelled. Any payment held is refunded in full.");
        setOpen(false);
      },
    });
  }

  return (
    <>
      <Button
        variant="destructive"
        data-testid="cancel-booking"
        onClick={() => setOpen(true)}
      >
        Cancel booking
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Cancel this booking?</DialogTitle>
            <DialogDescription>
              The concert is removed and any payment held is refunded in full.
              This can't be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setOpen(false)}
              disabled={cancel.isPending}
            >
              Keep booking
            </Button>
            <Button
              variant="destructive"
              data-testid="cancel-booking-confirm"
              onClick={handleConfirm}
              disabled={cancel.isPending}
            >
              {cancel.isPending ? "Cancelling..." : "Cancel booking"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
