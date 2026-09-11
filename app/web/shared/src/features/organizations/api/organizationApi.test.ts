import { beforeEach, describe, expect, it, vi } from "vitest";
import organizationApi from "./organizationApi";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: {
    get: mocks.get,
    put: mocks.put,
  },
}));

describe("organizationApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("returns null, not undefined, when the tenant has no organization yet", async () => {
    mocks.get.mockResolvedValue({ data: null, status: 204 });

    const organization = await organizationApi.get();

    expect(organization).toBeNull();
  });

  it("returns the organization on a normal response", async () => {
    mocks.get.mockResolvedValue({ data: { id: 42 }, status: 200 });

    const organization = await organizationApi.get();

    expect(organization).toEqual({ id: 42 });
  });
});
