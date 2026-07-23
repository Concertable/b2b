import { useQuery } from "@tanstack/react-query";
import { useActiveTenantStore } from "@b2b/features/tenant";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(invitationId: string) {
  const setActiveTenant = useActiveTenantStore((s) => s.setActiveTenant);
  const { isError } = useQuery({
    queryKey: ["accept-invitation", invitationId],
    queryFn: async () => {
      const membership = await invitationApi.accept(invitationId);
      setActiveTenant(membership.tenantId);
      window.location.assign("/settings/members");
      return membership;
    },
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  return { isError };
}
