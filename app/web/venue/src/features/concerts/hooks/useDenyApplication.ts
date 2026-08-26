import { useState } from "react";
import { toast } from "sonner";
import { useRejectApplicationMutation } from "@concertable/web-b2b/features/concerts";

export function useDenyApplication(opportunityId: number) {
  const [target, setTarget] = useState<number>();
  const mutation = useRejectApplicationMutation(opportunityId);

  function confirm() {
    if (target == null) return;
    mutation.mutate(target, {
      onSuccess: () => {
        toast.success("Application denied.");
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
