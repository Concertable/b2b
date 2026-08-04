import { userManager } from "@/features/auth";
import { apiClient } from "@concertable/shared/lib/apiClient";
import { paymentClient } from "@concertable/shared/lib/paymentClient";
import { configureWebClient } from "shared/lib/configureWebClient";
import { TENANT_HEADER } from "../features/tenant";
import { tenantSession } from "../features/tenant/tenantSession";

configureWebClient(apiClient, import.meta.env.VITE_API_URL).withTenant(
  tenantSession.tenantIdForRequest,
  TENANT_HEADER,
);
configureWebClient(paymentClient, import.meta.env.VITE_PAYMENT_API_URL).withTenant(
  tenantSession.tenantIdForRequest,
  TENANT_HEADER,
);

userManager.events.addUserUnloaded(tenantSession.clear);
