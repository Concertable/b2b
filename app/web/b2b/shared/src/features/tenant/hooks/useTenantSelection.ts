import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import {
  getActiveTenantId,
  setActiveTenant,
  useActiveTenantId,
} from "../activeTenant";
import { getCachedMemberships } from "../identityCache";
import { hasPendingTenantChoice } from "../memberships";
import type { TenantType } from "../types";
import { useMemberships } from "./useMemberships";

export function useTenantChoicePending(persona: TenantType): boolean {
  const memberships = useMemberships(persona);
  const activeTenantId = useActiveTenantId();
  return hasPendingTenantChoice(memberships, persona, activeTenantId);
}

export function isTenantChoicePending(persona: TenantType): boolean {
  return hasPendingTenantChoice(
    getCachedMemberships(),
    persona,
    getActiveTenantId(),
  );
}

export function useSelectTenant(): (tenantId: string) => void {
  const router = useRouter();
  const queryClient = useQueryClient();
  return useCallback(
    (tenantId: string) => {
      setActiveTenant(tenantId);
      void router.invalidate();
      void queryClient.invalidateQueries();
    },
    [queryClient, router],
  );
}
