import { useState } from "react";
import { toast } from "sonner";
import { useRejectApplicationMutation } from "@b2b/features/concerts";

export function useDenyApplication(opportunityId: number) {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useRejectApplicationMutation(opportunityId);

  async function confirm() {
    if (target == null) return;
    try {
      await mutation.mutateAsync(target);
      toast.success("Application denied.");
      setTarget(null);
    } catch {
      toast.error("Couldn't deny this application. Please try again.");
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
