import { beforeEach, describe, expect, it, vi } from "vitest";
import conversationApi from "./conversationApi";

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
  post: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: { get: mocks.get, put: mocks.put, post: mocks.post },
}));

describe("conversationApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("gets a page of conversation previews", async () => {
    const previews = [{ id: 42 }];
    const signal = new AbortController().signal;
    mocks.get.mockResolvedValue({ data: previews });

    await expect(conversationApi.getPreviews(2, signal)).resolves.toBe(previews);
    expect(mocks.get).toHaveBeenCalledWith("/conversations/previews", {
      params: { pageNumber: 2 },
      signal,
    });
  });

  it("gets the unread count and a conversation's messages", async () => {
    mocks.get.mockResolvedValueOnce({ data: 3 }).mockResolvedValueOnce({ data: [] });

    await expect(conversationApi.getUnreadCount()).resolves.toBe(3);
    await expect(conversationApi.getMessages(7)).resolves.toEqual([]);
    expect(mocks.get).toHaveBeenNthCalledWith(1, "/conversations/unread-count", {
      signal: undefined,
    });
    expect(mocks.get).toHaveBeenNthCalledWith(2, "/conversations/7/messages", {
      signal: undefined,
    });
  });

  it("advances a read position with only the sequence in the body", async () => {
    mocks.put.mockResolvedValue({});

    await conversationApi.advanceReadPosition(7, { throughSequence: 2 });
    expect(mocks.put).toHaveBeenCalledWith("/conversations/7/read-position", {
      throughSequence: 2,
    });
  });

  it("reports a message addressed by its conversation and message", async () => {
    mocks.post.mockResolvedValue({});
    const request = { category: "spam" as const, details: "Unwanted" };

    await conversationApi.reportMessage(7, 11, request);
    expect(mocks.post).toHaveBeenCalledWith(
      "/conversations/7/messages/11/report",
      request,
    );
  });
});
