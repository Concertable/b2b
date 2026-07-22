import { useState } from "react";
import { toast } from "sonner";
import { useCancelApplicationMutation } from "@b2b/features/concerts";

export function useCancelApplication(opportunityId: number) {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useCancelApplicationMutation(opportunityId);

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success(
          "Application cancelled. Any payment held is refunded in full.",
        );
        setTarget(null);
      },
    });
  }

  return {
    isOpen: target != null,
    request: setTarget,
    dismiss: () => setTarget(null),
    confirm,
    isPending: mutation.isPending,
  };
}
