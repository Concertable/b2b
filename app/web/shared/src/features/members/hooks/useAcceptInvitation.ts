import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  b2bIdentityKeys,
  identityApi,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import type { TenantType } from "@concertable/b2b/features/tenant/types";
import { acceptInvitation } from "../acceptInvitation";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(
  invitationId: string,
  tenantType: TenantType,
) {
  const queryClient = useQueryClient();
  const { isError } = useQuery({
    queryKey: ["accept-invitation", invitationId],
    queryFn: () =>
      acceptInvitation(invitationId, {
        accept: invitationApi.accept,
        // Deliberately not the tenant hook's selectTenant: it awaits a global query
        // invalidation, which from inside this queryFn would await this query's own refetch.
        selectTenant: async (tenantId) => {
          await queryClient.fetchQuery({
            queryKey: b2bIdentityKeys.all(),
            queryFn: identityApi.getMe,
            staleTime: 0,
          });
          await tenantSession.select(tenantId);
          await tenantSession.resolve(tenantType);
        },
        navigate: (path) => window.location.assign(path),
      }),
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  return { isError };
}
