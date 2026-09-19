import axios, {
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TENANT_HEADER } from "./constants";
import { installTenantSessionInterceptors } from "./tenantHttp";
import {
  StaleTenantSessionError,
  TenantSwitchInProgressError,
  tenantSession,
} from "./tenantSession";
import type { Membership, TenantStorage } from "./types";

const memberships: ReadonlyArray<Membership> = [
  {
    membershipId: "membership-one",
    tenantId: "tenant-one",
    legalName: "Tenant One",
    role: "owner",
    permissionVersion: 1,
    businessActivities: [],
    permissions: ["tenant.settings.edit"],
  },
  {
    membershipId: "membership-two",
    tenantId: "tenant-two",
    legalName: "Tenant Two",
    role: "owner",
    permissionVersion: 2,
    businessActivities: [],
    permissions: ["tenant.settings.edit"],
  },
];

async function configureSession() {
  let activeTenantId: string | undefined = "tenant-one";
  const storage: TenantStorage = {
    loadActiveTenantId: () => activeTenantId,
    saveActiveTenantId: (tenantId) => {
      activeTenantId = tenantId;
    },
    clearActiveTenantId: () => {
      activeTenantId = undefined;
    },
  };
  await tenantSession.configure({
    storage,
    memberships: () => memberships,
    clearMemberships: vi.fn(),
  });
}

const responseFor = (
  config: InternalAxiosRequestConfig,
): AxiosResponse<string> => ({
  config,
  data: "ok",
  headers: {},
  status: 200,
  statusText: "OK",
});

describe("tenant HTTP fencing", () => {
  afterEach(() => tenantSession.clear());

  it("sends the captured tenant and discards its late response", async () => {
    await configureSession();
    let releaseResponse: (() => void) | undefined;
    const responseBlocked = new Promise<void>((resolve) => {
      releaseResponse = resolve;
    });
    let capturedConfig: InternalAxiosRequestConfig | undefined;
    const client = axios.create({
      adapter: async (config) => {
        capturedConfig = config;
        await responseBlocked;
        return responseFor(config);
      },
    });
    installTenantSessionInterceptors(client, TENANT_HEADER);

    const request = client.get("/resource");
    await vi.waitFor(() => expect(capturedConfig).toBeDefined());
    expect(capturedConfig?.headers.get(TENANT_HEADER)).toBe("tenant-one");

    await tenantSession.switchTo("tenant-two", { prepare: () => undefined });
    releaseResponse?.();

    await expect(request).rejects.toBeInstanceOf(StaleTenantSessionError);
  });

  it("rejects a new mutation while the session is switching", async () => {
    await configureSession();
    let releasePreparation: (() => void) | undefined;
    const preparation = new Promise<void>((resolve) => {
      releasePreparation = resolve;
    });
    const client = axios.create({
      adapter: async (config) => responseFor(config),
    });
    installTenantSessionInterceptors(client, TENANT_HEADER);

    const switching = tenantSession.switchTo("tenant-two", {
      prepare: () => preparation,
    });
    await expect(client.post("/resource")).rejects.toBeInstanceOf(
      TenantSwitchInProgressError,
    );
    releasePreparation?.();
    await switching;
  });
});
