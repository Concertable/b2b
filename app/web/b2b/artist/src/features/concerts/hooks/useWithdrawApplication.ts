import { useState } from "react";
import { toast } from "sonner";
import { useWithdrawApplicationMutation } from "@b2b/features/concerts";

export function useWithdrawApplication() {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useWithdrawApplicationMutation();

  async function confirm() {
    if (target == null) return;
    try {
      await mutation.mutateAsync(target);
      toast.success("Application withdrawn.");
      setTarget(null);
    } catch {
      toast.error("Couldn't withdraw this application. Please try again.");
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
