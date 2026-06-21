import { AxiosError, type AxiosInstance } from "axios";
import { userManager } from "@/features/auth";
import api, { configureApi } from "@concertable/shared/lib/axiosClient";
import paymentApi, { configurePaymentApi } from "@concertable/shared/lib/paymentAxiosClient";
import { TENANT_HEADER, useActiveTenantStore } from "../features/tenant";

/* The B2B axios client. The customer app uses shared/lib/axios + shared/lib/paymentAxios; the manager apps
   side-effect-import this instead, so the tenant concept never reaches the customer bundle. It configures the
   SAME api + paymentApi instances every api module already imports (both pointed at the B2B host for managers)
   and stamps the active-tenant header, so every B2B call — including the payout proxy on paymentApi — carries
   X-Tenant-Id. No-op until the Phase-6 switcher selects a tenant. */
configureApi(import.meta.env.VITE_API_URL);
configurePaymentApi(import.meta.env.VITE_PAYMENT_API_URL);

function attach(client: AxiosInstance) {
  client.interceptors.request.use(async (config) => {
    const user = await userManager.getUser();
    if (user?.access_token) config.headers.Authorization = `Bearer ${user.access_token}`;

    const tenantId = useActiveTenantStore.getState().activeTenantId;
    if (tenantId) config.headers[TENANT_HEADER] = tenantId;

    return config;
  });

  client.interceptors.response.use(
    (res) => res,
    async (error: AxiosError) => {
      if (error.response?.status === 401) await userManager.removeUser();
      return Promise.reject(error);
    },
  );
}

attach(api);
attach(paymentApi);

/* A persisted activeTenantId must not outlive its user: if A selects a tenant, logs out, and B logs in on the
   same browser, the stale id would replay as X-Tenant-Id. removeUser() — explicit logout and the 401 handler
   alike — fires UserUnloaded, so clear the selection there. */
userManager.events.addUserUnloaded(() => useActiveTenantStore.getState().setActiveTenant(null));
