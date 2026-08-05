import { useState } from "react";
import { toast } from "sonner";
import { useWithdrawApplicationMutation } from "@concertable/b2b/web/shared/features/concerts";

export function useWithdrawApplication() {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useWithdrawApplicationMutation();

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success("Application withdrawn.");
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
