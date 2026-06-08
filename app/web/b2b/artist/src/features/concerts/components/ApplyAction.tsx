import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import type { Opportunity } from "@/features/concerts";
import { useApply } from "../hooks/useApply";

interface Props {
  opportunity: Opportunity;
}

export function ApplyAction({ opportunity }: Readonly<Props>) {
  const navigate = useNavigate();
  const { apply, isPending, error, canApply } = useApply(opportunity.id, {
    onSuccess: () => toast.success("Application submitted!"),
  });

  return (
    <div className="flex flex-col items-end gap-1">
      <Button
        size="sm"
        disabled={!canApply || isPending}
        data-testid="apply"
        onClick={() =>
          opportunity.actions.checkout != null
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
