import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import {
  b2bIdentityKeys,
  identityApi,
  tenantSession,
  useB2bIdentityQuery,
  useTenant as useCoreTenant,
} from "@concertable/b2b/features/tenant";
import type { TenantType } from "@concertable/b2b/features/tenant/types";

export function useTenantIdentity() {
  return useB2bIdentityQuery();
}

export function useTenant(tenantType: TenantType) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: identity } = useTenantIdentity();
  const tenant = useCoreTenant(identity?.memberships ?? [], tenantType);

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
      await tenantSession.select(tenantId);
      await Promise.all([router.invalidate(), queryClient.invalidateQueries()]);
    },
    [identity, queryClient, router],
  );

  return { ...tenant, selectTenant };
}
