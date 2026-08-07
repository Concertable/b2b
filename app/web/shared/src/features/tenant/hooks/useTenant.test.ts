import { beforeEach, describe, expect, it, vi } from "vitest";
import { useTenant } from "./useTenant";

const mocks = vi.hoisted(() => ({
  fetchQuery: vi.fn(),
  getMe: vi.fn(),
  identity: {
    memberships: [
      {
        tenantId: "existing-tenant",
        legalName: "Existing Venue",
        type: "Venue",
        role: "Staff",
      },
    ],
  },
  invalidateQueries: vi.fn(),
  invalidateRouter: vi.fn(),
  selectInStore: vi.fn(),
  synchronizeTenant: vi.fn(),
}));

vi.mock("react", () => ({
  useCallback: (callback: unknown) => callback,
  useEffect: vi.fn(),
}));
vi.mock("@tanstack/react-query", () => ({
  useQuery: () => ({ data: mocks.identity }),
  useQueryClient: () => ({
    fetchQuery: mocks.fetchQuery,
    invalidateQueries: mocks.invalidateQueries,
  }),
}));
vi.mock("@tanstack/react-router", () => ({
  useRouter: () => ({ invalidate: mocks.invalidateRouter }),
}));

vi.mock("@concertable/web/features/user", () => ({ useSyncUser: vi.fn() }));
vi.mock("@concertable/web/features/user/hooks/useSyncUser", () => ({
  meQueryKey: ["auth", "me"],
}));
vi.mock("../api/identityApi", () => ({
  default: { getMe: mocks.getMe },
}));
vi.mock("../memberships", () => ({
  resolveTenant: vi.fn(() => ({
    activeMembership: undefined,
    activeTenant: undefined,
    memberships: [],
    selectionRequired: false,
  })),
}));
vi.mock("../permissions", () => ({
  permissionsForRole: vi.fn(() => ({})),
}));
vi.mock("../store/useTenantStore", () => ({
  useTenantStore: (
    selector: (state: {
      activeTenantId: string;
      selectTenant: typeof mocks.selectInStore;
      synchronizeTenant: typeof mocks.synchronizeTenant;
    }) => unknown,
  ) =>
    selector({
      activeTenantId: "existing-tenant",
      selectTenant: mocks.selectInStore,
      synchronizeTenant: mocks.synchronizeTenant,
    }),
}));

describe("useTenant selection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.fetchQuery.mockResolvedValue(undefined);
    mocks.invalidateQueries.mockResolvedValue(undefined);
    mocks.invalidateRouter.mockResolvedValue(undefined);
  });

  it("refreshes identity before selecting a newly available tenant", async () => {
    const order: string[] = [];
    mocks.fetchQuery.mockImplementation(async () => {
      order.push("refresh");
    });
    mocks.selectInStore.mockImplementation(() => order.push("select"));
    mocks.invalidateRouter.mockImplementation(async () => {
      order.push("router");
    });
    mocks.invalidateQueries.mockImplementation(async () => {
      order.push("queries");
    });

    const { selectTenant } = useTenant("Venue");
    await selectTenant("accepted-tenant");

    expect(mocks.fetchQuery).toHaveBeenCalledWith({
      queryKey: ["auth", "me"],
      queryFn: mocks.getMe,
      staleTime: 0,
    });
    expect(order[0]).toBe("refresh");
    expect(order[1]).toBe("select");
    expect(order.slice(2)).toEqual(expect.arrayContaining(["router", "queries"]));
  });
});
