import { describe, expect, it } from "vitest";
import { getReadPosition } from "./readPositions";
import type { ConversationMessage } from "./types";

const message = (conversationId: number, sequence: number): ConversationMessage => ({
  id: conversationId * 100 + sequence,
  conversationId,
  sequence,
  senderTenantId: "tenant",
  sentByUserId: "user",
  content: "content",
  sentAt: "2026-09-27T00:00:00Z",
  actions: {},
});

describe("getReadPosition", () => {
  it("reads through the highest delivered sequence", () => {
    expect(getReadPosition(7, [message(7, 1), message(7, 3), message(7, 2)])).toEqual({
      conversationId: 7,
      throughSequence: 3,
    });
  });

  it("has nothing to acknowledge when no message was delivered", () => {
    expect(getReadPosition(7, [])).toBeUndefined();
  });

  it("ignores another conversation's messages", () => {
    expect(getReadPosition(7, [message(8, 5)])).toBeUndefined();
    expect(getReadPosition(7, [message(7, 1), message(8, 5)])).toEqual({
      conversationId: 7,
      throughSequence: 1,
    });
  });
});
