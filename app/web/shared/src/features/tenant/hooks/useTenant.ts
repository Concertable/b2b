import { useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import {
  meQueryKey,
  useMeQuery,
  useSyncUser,
} from "@concertable/web/features/user";
import identityApi from "../api/identityApi";
import { resolveTenant } from "../memberships";
import { permissionsForRole } from "../permissions";
import { useTenantStore } from "../store/useTenantStore";
import type { TenantType } from "../types";

export function useTenantIdentity() {
  useSyncUser(identityApi.getMe);
  return useMeQuery(identityApi.getMe);
}

export function useTenant(tenantType: TenantType) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const activeTenantId = useTenantStore((state) => state.activeTenantId);
  const selectInStore = useTenantStore((state) => state.selectTenant);
  const synchronizeTenant = useTenantStore(
    (state) => state.synchronizeTenant,
  );
  const { data: identity } = useTenantIdentity();
  const resolution = resolveTenant(
    identity?.memberships ?? [],
    tenantType,
    activeTenantId,
  );

  useEffect(() => {
    if (identity) synchronizeTenant(identity.memberships, tenantType);
  }, [identity, tenantType, synchronizeTenant]);

  const selectTenant = useCallback(
    async (tenantId: string) => {
      if (
        !identity?.memberships.some(
          (membership) => membership.tenantId === tenantId,
        )
      ) {
        await queryClient.fetchQuery({
          queryKey: meQueryKey,
          queryFn: identityApi.getMe,
          staleTime: 0,
        });
      }
      selectInStore(tenantId);
      void router.invalidate();
      void queryClient.invalidateQueries();
    },
    [identity, queryClient, router, selectInStore],
  );

  return {
    ...resolution,
    permissions: permissionsForRole(resolution.activeMembership?.role),
    selectTenant,
  };
}
