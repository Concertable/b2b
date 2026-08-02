import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { hasPendingTenantChoice } from "../memberships";
import { useActiveTenantStore } from "../store/useActiveTenantStore";
import type { TenantType } from "../types";
import { useMemberships } from "./useMemberships";

export function useTenantChoicePending(persona: TenantType): boolean {
  const memberships = useMemberships(persona);
  const activeTenantId = useActiveTenantStore((state) => state.activeTenantId);
  return hasPendingTenantChoice(memberships, persona, activeTenantId);
}

export function useSelectTenant(): (tenantId: string) => void {
  const router = useRouter();
  const queryClient = useQueryClient();
  const setActiveTenant = useActiveTenantStore(
    (state) => state.setActiveTenant,
  );
  return useCallback(
    (tenantId: string) => {
      setActiveTenant(tenantId);
      void router.invalidate();
      void queryClient.invalidateQueries();
    },
    [queryClient, router, setActiveTenant],
  );
}
