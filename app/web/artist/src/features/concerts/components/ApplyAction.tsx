import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { Opportunity } from "@/features/concerts";
import { ESignaturePanel, type ESignatureRequest } from "@b2b/features/concerts";
import { useApply } from "../hooks/useApply";

interface Props {
  opportunity: Opportunity;
}

export function ApplyAction({ opportunity }: Readonly<Props>) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [eSignature, setESignature] = useState<ESignatureRequest>({ signatoryName: "" });
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
            contract={opportunity.contract}
            value={eSignature}
            onChange={setESignature}
          />
          <Button
            disabled={isPending || eSignature.signatoryName.trim() === ""}
            data-testid="confirm-apply"
            onClick={() => apply(eSignature)}
          >
            {isPending ? "Applying..." : "Sign & Apply"}
          </Button>
        </DialogContent>
      </Dialog>
    </div>
  );
}
