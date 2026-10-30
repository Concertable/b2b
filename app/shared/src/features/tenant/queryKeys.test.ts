import { describe, expect, it } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { isTenantSwitchQuery, privateQueryKey } from "./queryKeys";

describe("privateQueryKey", () => {
  it("starts with tenant, membership and permission identity", () => {
    expect(
      privateQueryKey(
        {
          generation: 8,
          tenantId: "tenant-1",
          membershipId: "membership-1",
          permissionVersion: 4,
          rolePolicyVersion: 1,
        },
        "concert",
        12,
        "summary",
      ),
    ).toEqual([
      "tenant",
      "tenant-1",
      "membership-1",
      4,
      1,
      "concert",
      12,
      "summary",
    ]);
  });
});


describe("isTenantSwitchQuery", () => {
  it("clears legacy tenant scoped caches while keeping identity", () => {
    const client = new QueryClient();
    client.setQueryData(["messages", "unread-count"], 3);
    client.setQueryData(["concerts", "venue", "application", 42], { id: 7 });
    client.setQueryData(["auth", "me"], { memberships: [] });
    client.setQueryData(["invitation", "invite-1", "accept"], true);

    const queries = client.getQueryCache().getAll();
    expect(queries.filter(isTenantSwitchQuery).map((query) => query.queryKey)).toEqual([
      ["messages", "unread-count"],
      ["concerts", "venue", "application", 42],
    ]);
  });
});
