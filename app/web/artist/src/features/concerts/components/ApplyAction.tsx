import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { Button } from "@concertable/web/shared/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@concertable/web/shared/components/ui/dialog";
import type { Opportunity } from "@concertable/b2b/web/shared/features/concerts";
import { ESignaturePanel, useESignature } from "@concertable/b2b/web/shared/features/concerts";
import { useApply } from "../hooks/useApply";

interface Props {
  opportunity: Opportunity;
}

export function ApplyAction({ opportunity }: Readonly<Props>) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const { signature, setSignature, isValid } = useESignature();
  const { apply, isPending, error, canApply } = useApply(opportunity.id, {
    onSuccess: () => {
      setOpen(false);
      toast.success("Application submitted!");
    },
  });

  const requiresCheckout = opportunity.actions.checkout != null;

  return (
    <div className="flex flex-col items-end gap-2">
      <Button
        size="sm"
        disabled={!canApply || isPending}
        data-testid="apply"
        onClick={() =>
          requiresCheckout
            ? navigate({
                to: "/opportunity/checkout/$opportunityId",
                params: { opportunityId: opportunity.id },
              })
            : setOpen(true)
        }
      >
        {isPending ? "Applying..." : "Apply"}
      </Button>
      {error && <p className="text-destructive text-sm">{error.message}</p>}

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Sign &amp; apply</DialogTitle>
          </DialogHeader>
          <ESignaturePanel
            deal={opportunity.deal}
            value={signature}
            onChange={setSignature}
          />
          <Button
            disabled={isPending || !isValid}
            data-testid="confirm-apply"
            onClick={() => apply(signature)}
          >
            {isPending ? "Applying..." : "Sign & Apply"}
          </Button>
        </DialogContent>
      </Dialog>
    </div>
  );
}
