import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  b2bIdentityKeys,
  identityApi,
  isPrivateQuery,
  settlePendingMutations,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import type { TenantBusinessActivity } from "@concertable/b2b/features/tenant/types";
import { acceptInvitation } from "../acceptInvitation";
import invitationApi from "../api/invitationApi";

export function useAcceptInvitation(
  invitationId: string,
  businessActivity?: TenantBusinessActivity,
) {
  const queryClient = useQueryClient();
  const { isError } = useQuery({
    queryKey: ["invitation", invitationId, "accept"],
    queryFn: () =>
      acceptInvitation(invitationId, {
        accept: invitationApi.accept,
        selectTenant: async (tenantId) => {
          await queryClient.fetchQuery({
            queryKey: b2bIdentityKeys.all(),
            queryFn: identityApi.getMe,
            staleTime: 0,
          });
          await tenantSession.switchTo(tenantId, {
            prepare: async () => {
              await queryClient.cancelQueries({ predicate: isPrivateQuery });
              await settlePendingMutations(queryClient);
              queryClient.removeQueries({ predicate: isPrivateQuery });
            },
          });
          await tenantSession.resolve(businessActivity);
        },
        navigate: (path) => window.location.assign(path),
      }),
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  return { isError };
}
