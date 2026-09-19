import type { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from "axios";
import { StaleTenantSessionError, tenantSession } from "./tenantSession";
import type { TenantSession } from "./types";

const SAFE_METHODS = new Set(["get", "head", "options"]);
const capturedSessions = new WeakMap<InternalAxiosRequestConfig, TenantSession>();

export function installTenantSessionInterceptors(
  client: AxiosInstance,
  tenantHeader: string,
) {
  client.interceptors.request.use((request) => {
    const method = request.method?.toLowerCase() ?? "get";
    const session = tenantSession.captureRequest(!SAFE_METHODS.has(method));
    if (session !== undefined) {
      request.headers.set(tenantHeader, session.tenantId);
      capturedSessions.set(request, session);
    }
    return request;
  });

  client.interceptors.response.use(
    (response) => {
      const session = capturedSessions.get(response.config);
      if (session !== undefined && !tenantSession.isCurrent(session))
        return Promise.reject(new StaleTenantSessionError());
      return response;
    },
    (error: AxiosError) => {
      const session = error.config && capturedSessions.get(error.config);
      return Promise.reject(
        session !== undefined && !tenantSession.isCurrent(session)
          ? new StaleTenantSessionError()
          : error,
      );
    },
  );
}
