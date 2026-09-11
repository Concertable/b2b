import * as SecureStore from "expo-secure-store";
import type { TenantStorage } from "@concertable/b2b/features/tenant/types";

const ACTIVE_TENANT_KEY = "concertable.active-tenant";

export const tenantStorage: TenantStorage = {
  loadActiveTenantId: async () =>
    (await SecureStore.getItemAsync(ACTIVE_TENANT_KEY)) ?? undefined,
  saveActiveTenantId: (tenantId) =>
    SecureStore.setItemAsync(ACTIVE_TENANT_KEY, tenantId),
  clearActiveTenantId: () =>
    SecureStore.deleteItemAsync(ACTIVE_TENANT_KEY),
};
