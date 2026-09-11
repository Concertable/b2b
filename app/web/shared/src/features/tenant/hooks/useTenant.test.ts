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
        type: "venue" as const,
        role: "staff" as const,
      },
    ],
  },
  invalidateQueries: vi.fn(),
  invalidateRouter: vi.fn(),
  selectTenant: vi.fn(),
}));

vi.mock("react", () => ({
  useCallback: (callback: unknown) => callback,
}));
vi.mock("@tanstack/react-query", () => ({
  useQueryClient: () => ({
    fetchQuery: mocks.fetchQuery,
    invalidateQueries: mocks.invalidateQueries,
  }),
}));
vi.mock("@tanstack/react-router", () => ({
  useRouter: () => ({ invalidate: mocks.invalidateRouter }),
}));
vi.mock("@concertable/b2b/features/tenant", () => ({
  b2bIdentityKeys: { all: () => ["auth", "me"] },
  identityApi: { getMe: mocks.getMe },
  tenantSession: { select: mocks.selectTenant },
  useB2bIdentityQuery: () => ({ data: mocks.identity }),
  useTenant: () => ({
    activeMembership: undefined,
    memberships: [],
    permissions: new Set(),
    selectionRequired: false,
  }),
}));

describe("web tenant selection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.fetchQuery.mockResolvedValue(undefined);
    mocks.selectTenant.mockResolvedValue(undefined);
    mocks.invalidateQueries.mockResolvedValue(undefined);
    mocks.invalidateRouter.mockResolvedValue(undefined);
  });

  it("refreshes identity before selecting a newly available tenant", async () => {
    const order: string[] = [];
    mocks.fetchQuery.mockImplementation(async () => {
      order.push("refresh");
    });
    mocks.selectTenant.mockImplementation(async () => {
      order.push("select");
    });
    mocks.invalidateRouter.mockImplementation(async () => {
      order.push("router");
    });
    mocks.invalidateQueries.mockImplementation(async () => {
      order.push("queries");
    });

    const { selectTenant } = useTenant("venue");
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
