import { useState } from "react";
import { toast } from "sonner";
import { useWithdrawApplicationMutation } from "@concertable/web-b2b/features/concerts";

export function useWithdrawApplication() {
  const [target, setTarget] = useState<number>();
  const mutation = useWithdrawApplicationMutation();

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success("Application withdrawn.");
        setTarget(undefined);
      },
    });
  }

  return {
    isOpen: target != null,
    request: setTarget,
    dismiss: () => setTarget(undefined),
    confirm,
    isPending: mutation.isPending,
  };
}
