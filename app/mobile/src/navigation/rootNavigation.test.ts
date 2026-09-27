import { describe, expect, it } from "vitest";
import { PUBLIC_TAB_NAMES, rootNavigationTarget } from "./rootNavigation";

describe("root navigation", () => {
  it("keeps private routes out of an unauthenticated session", () => {
    expect(rootNavigationTarget(false)).toBe("public");
    expect(Object.values(PUBLIC_TAB_NAMES)).toEqual([
      "Home",
      "Search",
      "Account",
    ]);
    expect(Object.values(PUBLIC_TAB_NAMES)).not.toEqual(
      expect.arrayContaining(["MyArtistTab", "Messages", "ProfileTab"]),
    );
  });

  it("selects authenticated navigation for an authenticated session", () => {
    expect(rootNavigationTarget(true)).toBe("authenticated");
  });
});
