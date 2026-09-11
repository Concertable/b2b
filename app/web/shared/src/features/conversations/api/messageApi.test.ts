import { beforeEach, describe, expect, it, vi } from "vitest";
import messageApi from "./messageApi";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: { get: mocks.get },
}));

describe("messageApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("gets recent message previews from the Message resource", async () => {
    const previews = [{ id: 42 }];
    mocks.get.mockResolvedValue({ data: previews });

    await expect(messageApi.getPreviews()).resolves.toBe(previews);
    expect(mocks.get).toHaveBeenCalledWith("/message/previews");
  });
});
