import { useState } from "react";
import { toast } from "sonner";
import { useCancelApplicationMutation } from "@b2b/features/concerts";

export function useCancelApplication(opportunityId: number) {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useCancelApplicationMutation(opportunityId);

  async function confirm() {
    if (target == null) return;
    try {
      await mutation.mutateAsync(target);
      toast.success(
        "Application cancelled. Any payment held is refunded in full.",
      );
      setTarget(null);
    } catch {
      toast.error("Couldn't cancel this application. Please try again.");
    }
  }

  return {
    isOpen: target != null,
    request: setTarget,
    dismiss: () => setTarget(null),
    confirm,
    isPending: mutation.isPending,
  };
}
