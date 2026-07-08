import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import type { Opportunity } from "@/features/concerts";
import { AgreeToTermsCheckbox } from "@b2b/features/concerts";
import { useApply } from "../hooks/useApply";

interface Props {
  opportunity: Opportunity;
}

export function ApplyAction({ opportunity }: Readonly<Props>) {
  const navigate = useNavigate();
  const [agreed, setAgreed] = useState(false);
  const { apply, isPending, error, canApply } = useApply(opportunity.id, {
    onSuccess: () => toast.success("Application submitted!"),
  });

  const requiresCheckout = opportunity.actions.checkout != null;

  return (
    <div className="flex flex-col items-end gap-2">
      {!requiresCheckout && canApply && (
        <AgreeToTermsCheckbox checked={agreed} onCheckedChange={setAgreed} />
      )}
      <Button
        size="sm"
        disabled={!canApply || isPending || (!requiresCheckout && !agreed)}
        data-testid="apply"
        onClick={() =>
          requiresCheckout
            ? navigate({
                to: "/opportunity/checkout/$opportunityId",
                params: { opportunityId: opportunity.id },
              })
            : apply()
        }
      >
        {isPending ? "Applying..." : "Apply"}
      </Button>
      {error && <p className="text-destructive text-sm">{error.message}</p>}
    </div>
  );
}
