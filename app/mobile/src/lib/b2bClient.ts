import {
  b2bIdentityKeys,
  TENANT_HEADER,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import type { B2bIdentity } from "@concertable/b2b/features/tenant/types";
import { mobileAuthSession } from "@concertable/mobile/auth/mobileAuthSession";
import { apiClient } from "@concertable/mobile/lib/apiClient";
import Config from "@concertable/mobile/lib/config";
import { paymentClient } from "@concertable/mobile/lib/paymentClient";
import { queryClient } from "@concertable/mobile/providers/AppProviders";
import { configureClient } from "@concertable/shared/lib/client";
import { tenantStorage } from "../features/tenant/tenantStorage";

const tenantSessionConfiguration = {
  storage: tenantStorage,
  memberships: () =>
    queryClient.getQueryData<B2bIdentity>(b2bIdentityKeys.all())?.memberships ??
    [],
  clearMemberships: () =>
    queryClient.removeQueries({ queryKey: b2bIdentityKeys.all() }),
};

export function initializeTenantSession() {
  return tenantSession.configure(tenantSessionConfiguration);
}

configureClient(apiClient, `${Config.apiUrl}/api`).withTenant(
  tenantSession.tenantIdForRequest,
  TENANT_HEADER,
);
configureClient(paymentClient, `${Config.paymentApiUrl}/api`).withTenant(
  tenantSession.tenantIdForRequest,
  TENANT_HEADER,
);

mobileAuthSession.subscribe((user, previousUser) => {
  if (previousUser !== undefined && user === undefined) void tenantSession.clear();
});
