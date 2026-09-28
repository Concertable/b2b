import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import {
  b2bIdentityKeys,
  identityApi,
  isTenantSwitchQuery,
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
  const tenant = useCoreTenant(
    identity?.memberships ?? [],
    businessActivity,
    identity !== undefined,
  );

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
      let notificationsStopped = false;
      try {
        await tenantSession.switchTo(tenantId, {
          prepare: async () => {
            await queryClient.cancelQueries({ predicate: isTenantSwitchQuery });
            await notificationConnection.stop();
            notificationsStopped = true;
            await settlePendingMutations(queryClient);
            queryClient.removeQueries({ predicate: isTenantSwitchQuery });
          },
          activate: async () => {
            await notificationConnection.start();
            notificationsStopped = false;
          },
        });
      } catch (error) {
        if (notificationsStopped && tenantSession.current() !== undefined)
          await notificationConnection.start();
        throw error;
      }
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
