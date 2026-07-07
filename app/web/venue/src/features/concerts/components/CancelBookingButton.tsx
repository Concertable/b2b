import { useState } from "react";
import { toast } from "sonner";
import { useCancelConcert } from "@concertable/shared/features/concerts/hooks/useCancelConcert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

interface Props {
  concertId: number;
}

export function CancelBookingButton({ concertId }: Readonly<Props>) {
  const [open, setOpen] = useState(false);
  const cancel = useCancelConcert(concertId);

  async function handleConfirm() {
    try {
      await cancel.mutateAsync();
      toast.success("Booking cancelled. Any payment held is refunded in full.");
      setOpen(false);
    } catch {
      toast.error("Couldn't cancel this booking. Please try again.");
    }
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
              onClick={() => void handleConfirm()}
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
