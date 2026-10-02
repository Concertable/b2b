import { describe, expect, it } from "vitest";
import type { TenantPermission } from "@concertable/b2b/features/tenant/types";
import { includesOperationsTab } from "./businessNavigation";

describe("business navigation", () => {
  it("omits Operations without operations.view", () => {
    expect(includesOperationsTab(new Set<TenantPermission>())).toBe(false);
  });

  it("includes Operations with operations.view", () => {
    expect(
      includesOperationsTab(new Set<TenantPermission>(["operations.view"])),
    ).toBe(true);
  });
});
