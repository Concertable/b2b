import { userManager } from "@/features/auth";
import { apiClient } from "@concertable/shared/lib/apiClient";
import { paymentClient } from "@concertable/shared/lib/paymentClient";
import { configureWebClient } from "shared/lib/configureWebClient";
import { TENANT_HEADER, useActiveTenantStore } from "../features/tenant";

/* The B2B client wiring. The customer app configures apiClient + paymentClient with auth only; the manager
   apps side-effect-import this instead, so the tenant concept never reaches the customer bundle. It enhances
   the SAME apiClient + paymentClient instances every api module already imports (both pointed at the B2B host
   for managers) and stamps the active-tenant header, so every B2B call — including the payout proxy on
   paymentClient — carries X-Tenant-Id. No-op until the Phase-6 switcher selects a tenant. */
const getTenantId = () => useActiveTenantStore.getState().activeTenantId;

configureWebClient(apiClient, import.meta.env.VITE_API_URL).withTenant(getTenantId, TENANT_HEADER);
configureWebClient(paymentClient, import.meta.env.VITE_PAYMENT_API_URL).withTenant(getTenantId, TENANT_HEADER);

/* A persisted activeTenantId must not outlive its user: if A selects a tenant, logs out, and B logs in on the
   same browser, the stale id would replay as X-Tenant-Id. removeUser() — explicit logout and the 401 handler
   alike — fires UserUnloaded, so clear the selection there. */
userManager.events.addUserUnloaded(() => useActiveTenantStore.getState().setActiveTenant(null));
