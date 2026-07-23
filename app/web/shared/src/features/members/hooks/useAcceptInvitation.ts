import { useEffect, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useActiveTenantStore } from "@b2b/features/tenant";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(invitationId: string) {
  const setActiveTenant = useActiveTenantStore((s) => s.setActiveTenant);
  const [isError, setIsError] = useState(false);
  const fired = useRef(false);
  const { mutateAsync } = useMutation({ mutationFn: invitationApi.accept });

  useEffect(() => {
    if (fired.current) return;
    fired.current = true;
    // Await the promise, not a mutate() onSuccess: the accept fires on mount, so StrictMode's
    // remount disposes the mutation observer before the POST settles and a per-call callback is
    // dropped. The promise resolves regardless. Hard-navigate so /me re-fetches with the new
    // membership before the layout's tenant guard runs.
    mutateAsync(invitationId)
      .then((membership) => {
        setActiveTenant(membership.tenantId);
        window.location.assign("/settings/members");
      })
      .catch(() => setIsError(true));
  }, [invitationId, mutateAsync, setActiveTenant]);

  return { isError };
}
