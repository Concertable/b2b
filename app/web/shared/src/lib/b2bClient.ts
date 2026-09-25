import { userManager } from "@concertable/web/features/auth";
import { apiClient } from "@concertable/shared/lib/apiClient";
import { paymentClient } from "@concertable/shared/lib/paymentClient";
import { configureWebClient } from "@concertable/web/lib/configureWebClient";
import {
  installTenantSessionInterceptors,
  TENANT_HEADER,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import "../features/tenant/webTenantSession";

configureWebClient(apiClient, import.meta.env.VITE_API_URL);
configureWebClient(paymentClient, import.meta.env.VITE_PAYMENT_API_URL);
installTenantSessionInterceptors(apiClient, TENANT_HEADER);
installTenantSessionInterceptors(paymentClient, TENANT_HEADER);

userManager.events.addUserUnloaded(() => void tenantSession.clear());
