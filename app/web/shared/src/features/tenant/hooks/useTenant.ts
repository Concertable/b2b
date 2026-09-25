import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import {
  b2bIdentityKeys,
  identityApi,
  isPrivateQuery,
  settlePendingMutations,
  tenantSession,
  useB2bIdentityQuery,
  useTenant as useCoreTenant,
} from "@concertable/b2b/features/tenant";
import type { TenantBusinessActivity } from "@concertable/b2b/features/tenant/types";
import { notificationConnection } from "@concertable/web/lib/signalr";

export function useTenantIdentity() {
  return useB2bIdentityQuery();
}

export function useTenant(businessActivity?: TenantBusinessActivity) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: identity } = useTenantIdentity();
  const tenant = useCoreTenant(identity?.memberships ?? [], businessActivity);

  const selectTenant = useCallback(
    async (tenantId: string) => {
      if (
        !identity?.memberships.some(
          (membership) => membership.tenantId === tenantId,
        )
      ) {
        await queryClient.fetchQuery({
          queryKey: b2bIdentityKeys.all(),
          queryFn: identityApi.getMe,
          staleTime: 0,
        });
      }
      await tenantSession.switchTo(tenantId, {
        prepare: async () => {
          await queryClient.cancelQueries({ predicate: isPrivateQuery });
          await notificationConnection.stop();
          await settlePendingMutations(queryClient);
          queryClient.removeQueries({ predicate: isPrivateQuery });
        },
        activate: () => notificationConnection.start(),
      });
      await router.invalidate();
    },
    [identity, queryClient, router],
  );

  return {
    ...tenant,
    selectionRequired: tenant.isSelectionPending || tenant.selectionRequired,
    selectTenant,
  };
}
