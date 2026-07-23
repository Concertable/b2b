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
    mutateAsync(invitationId)
      .then((membership) => {
        setActiveTenant(membership.tenantId);
        window.location.assign("/settings/members");
      })
      .catch(() => setIsError(true));
  }, [invitationId, mutateAsync, setActiveTenant]);

  return { isError };
}
