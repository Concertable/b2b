import { beforeEach, describe, expect, it, vi } from "vitest";
import conversationApi from "./conversationApi";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: { get: mocks.get },
}));

describe("conversationApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("gets recent conversation previews", async () => {
    const previews = [{ id: 42 }];
    mocks.get.mockResolvedValue({ data: previews });

    await expect(conversationApi.getPreviews()).resolves.toBe(previews);
    expect(mocks.get).toHaveBeenCalledWith("/conversations/previews");
  });
});
