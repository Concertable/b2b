export const PUBLIC_TAB_NAMES = {
  home: "Home",
  search: "Search",
  account: "Account",
} as const;

export function rootNavigationTarget(isAuthenticated: boolean) {
  return isAuthenticated ? "authenticated" : "public";
}
