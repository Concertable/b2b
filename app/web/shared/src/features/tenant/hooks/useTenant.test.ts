import { beforeEach, describe, expect, it, vi } from "vitest";
import { useTenant } from "./useTenant";

const mocks = vi.hoisted(() => ({
  fetchQuery: vi.fn(),
  cancelQueries: vi.fn(),
  getMe: vi.fn(),
  identity: {
    memberships: [
      {
        membershipId: "existing-membership",
        tenantId: "existing-tenant",
        legalName: "Existing Venue",
        businessActivities: ["venueOperator"] as const,
        role: "staff" as const,
        permissionVersion: 1,
        permissions: ["operations.view"] as const,
      },
    ],
  },
  removeQueries: vi.fn(),
  invalidateRouter: vi.fn(),
  settlePendingMutations: vi.fn(),
  switchTenant: vi.fn(),
  startNotifications: vi.fn(),
  stopNotifications: vi.fn(),
}));

vi.mock("react", () => ({
  useCallback: (callback: unknown) => callback,
}));
vi.mock("@tanstack/react-query", () => ({
  useQueryClient: () => ({
    fetchQuery: mocks.fetchQuery,
    cancelQueries: mocks.cancelQueries,
    removeQueries: mocks.removeQueries,
  }),
}));
vi.mock("@tanstack/react-router", () => ({
  useRouter: () => ({ invalidate: mocks.invalidateRouter }),
}));
vi.mock("@concertable/b2b/features/tenant", () => ({
  b2bIdentityKeys: { all: () => ["auth", "me"] },
  identityApi: { getMe: mocks.getMe },
  isPrivateQuery: vi.fn(),
  settlePendingMutations: mocks.settlePendingMutations,
  tenantSession: { switchTo: mocks.switchTenant },
  useB2bIdentityQuery: () => ({ data: mocks.identity }),
  useTenant: () => ({
    activeMembership: undefined,
    memberships: [],
    permissions: new Set(),
    selectionRequired: false,
  }),
}));
vi.mock("@concertable/web/lib/signalr", () => ({
  notificationConnection: {
    start: mocks.startNotifications,
    stop: mocks.stopNotifications,
  },
}));

describe("web tenant selection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.fetchQuery.mockResolvedValue(undefined);
    mocks.cancelQueries.mockResolvedValue(undefined);
    mocks.settlePendingMutations.mockResolvedValue(undefined);
    mocks.switchTenant.mockImplementation(async (_tenantId, boundary) => {
      await boundary.prepare();
      await boundary.activate?.();
    });
    mocks.invalidateRouter.mockResolvedValue(undefined);
    mocks.startNotifications.mockResolvedValue(undefined);
    mocks.stopNotifications.mockResolvedValue(undefined);
  });

  it("refreshes identity before selecting a newly available tenant", async () => {
    const order: string[] = [];
    mocks.fetchQuery.mockImplementation(async () => {
      order.push("refresh");
    });
    mocks.switchTenant.mockImplementation(async (_tenantId, boundary) => {
      await boundary.prepare();
      order.push("select");
      await boundary.activate?.();
    });
    mocks.invalidateRouter.mockImplementation(async () => {
      order.push("router");
    });

    const { selectTenant } = useTenant("venueOperator");
    await selectTenant("accepted-tenant");

    expect(mocks.fetchQuery).toHaveBeenCalledWith({
      queryKey: ["auth", "me"],
      queryFn: mocks.getMe,
      staleTime: 0,
    });
    expect(order[0]).toBe("refresh");
    expect(order).toContain("select");
    expect(order.at(-1)).toBe("router");
  });

  it("restarts notifications around a tenant switch", async () => {
    const order: string[] = [];
    mocks.stopNotifications.mockImplementation(async () => order.push("stop"));
    mocks.switchTenant.mockImplementation(async (_tenantId, boundary) => {
      await boundary.prepare();
      order.push("select");
      await boundary.activate?.();
    });
    mocks.startNotifications.mockImplementation(async () => order.push("start"));

    const { selectTenant } = useTenant("venueOperator");
    await selectTenant("existing-tenant");

    expect(order).toEqual(["stop", "select", "start"]);
  });
});
