import { useQuery } from "@tanstack/react-query";
import { useTenant, type TenantType } from "@b2b/features/tenant";
import { acceptInvitation } from "../acceptInvitation";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(
  invitationId: string,
  persona: TenantType,
) {
  const { selectTenant } = useTenant(persona);
  const { isError } = useQuery({
    queryKey: ["accept-invitation", invitationId],
    queryFn: () =>
      acceptInvitation(invitationId, {
        accept: invitationApi.accept,
        selectTenant,
        navigate: (path) => window.location.assign(path),
      }),
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  return { isError };
}
