import { Button } from "@concertable/web/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@concertable/web/components/ui/dialog";

interface Props {
  open: boolean;
  title: string;
  description: string;
  dismissLabel: string;
  confirmLabel: string;
  pendingLabel: string;
  confirmTestId: string;
  isPending: boolean;
  onDismiss: () => void;
  onConfirm: () => void;
}

export function ConfirmActionDialog({
  open,
  title,
  description,
  dismissLabel,
  confirmLabel,
  pendingLabel,
  confirmTestId,
  isPending,
  onDismiss,
  onConfirm,
}: Readonly<Props>) {
  return (
    <Dialog open={open} onOpenChange={(o) => !o && onDismiss()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="ghost" onClick={onDismiss} disabled={isPending}>
            {dismissLabel}
          </Button>
          <Button
            variant="destructive"
            data-testid={confirmTestId}
            onClick={onConfirm}
            disabled={isPending}
          >
            {isPending ? pendingLabel : confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
