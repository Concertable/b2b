import { describe, expect, it } from "vitest";
import { privateQueryKey } from "./queryKeys";

describe("privateQueryKey", () => {
  it("starts with tenant, membership and permission identity", () => {
    expect(
      privateQueryKey(
        {
          generation: 8,
          tenantId: "tenant-1",
          membershipId: "membership-1",
          permissionVersion: 4,
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
      "concert",
      12,
      "summary",
    ]);
  });
});
