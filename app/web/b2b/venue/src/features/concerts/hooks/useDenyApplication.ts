import { useState } from "react";
import { toast } from "sonner";
import { useRejectApplicationMutation } from "@concertable/b2b/features/concerts";

export function useDenyApplication(opportunityId: number) {
  const [target, setTarget] = useState<number | null>(null);
  const mutation = useRejectApplicationMutation(opportunityId);

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success("Application denied.");
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
