import { useEffect, useRef } from "react";
import { useMutation } from "@tanstack/react-query";
import { useActiveTenantStore } from "@b2b/features/tenant";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(invitationId: string) {
  const setActiveTenant = useActiveTenantStore((s) => s.setActiveTenant);
  const fired = useRef(false);
  const { mutate, isError } = useMutation({
    mutationFn: invitationApi.accept,
  });

  useEffect(() => {
    if (fired.current) return;
    fired.current = true;
    mutate(invitationId, {
      onSuccess: (membership) => {
        // Hard-navigate so /me re-fetches with the new membership before the layout's tenant guard
        // runs (the in-memory copy is stale until then).
        setActiveTenant(membership.tenantId);
        window.location.assign("/settings/members");
      },
    });
  }, [invitationId, mutate, setActiveTenant]);

  return { isError };
}
