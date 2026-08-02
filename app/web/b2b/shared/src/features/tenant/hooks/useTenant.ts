import { useCallback, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { useStore } from "zustand";
import { useSyncUser } from "@/features/user";
import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import identityApi from "../api/identityApi";
import { resolveTenant } from "../memberships";
import { permissionsForRole } from "../permissions";
import { tenantStore } from "../store/tenantStore";
import type { B2bIdentity, TenantType } from "../types";

export function useTenantIdentity(): void {
  useSyncUser(identityApi.getMe);
}

export function useTenant(tenantType: TenantType) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const activeTenantId = useStore(
    tenantStore,
    (state) => state.activeTenantId,
  );
  const selectInStore = useStore(tenantStore, (state) => state.selectTenant);
  const synchronizeTenant = useStore(
    tenantStore,
    (state) => state.synchronizeTenant,
  );
  const { data: identity } = useQuery<B2bIdentity>({
    queryKey: meQueryKey,
    queryFn: identityApi.getMe,
    enabled: false,
  });
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
