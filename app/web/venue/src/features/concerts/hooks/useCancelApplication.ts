import { useState } from "react";
import { toast } from "sonner";
import { useCancelApplicationMutation } from "@concertable/web-b2b/features/concerts";

export function useCancelApplication(opportunityId: number) {
  const [target, setTarget] = useState<number>();
  const mutation = useCancelApplicationMutation(opportunityId);

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success("Application cancelled.");
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
