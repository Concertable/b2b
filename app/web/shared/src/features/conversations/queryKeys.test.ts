import { describe, expect, it } from "vitest";
import { conversationKeys } from "./queryKeys";

const session = {
  generation: 1,
  tenantId: "tenant-a",
  membershipId: "membership-a",
  permissionVersion: 1,
};

describe("conversationKeys", () => {
  it.each([
    { ...session, tenantId: "tenant-b" },
    { ...session, membershipId: "membership-b" },
    { ...session, permissionVersion: 2 },
  ])("never shares a key across tenant sessions", (other) => {
    expect(conversationKeys.unreadCount(other)).not.toEqual(
      conversationKeys.unreadCount(session),
    );
    expect(conversationKeys.messages(other, 7)).not.toEqual(
      conversationKeys.messages(session, 7),
    );
  });

  it("nests pages and messages under their invalidation prefixes", () => {
    expect(conversationKeys.previewPage(session, 2).slice(0, -1)).toEqual(
      conversationKeys.previews(session),
    );
    expect(
      conversationKeys.messages(session, 7).slice(0, conversationKeys.all(session).length),
    ).toEqual(conversationKeys.all(session));
  });
});
