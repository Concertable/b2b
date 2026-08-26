import { createRootRoute, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/router-devtools";
import { Toaster } from "@concertable/web/components/ui/sonner";
import { useSyncTenantIdentity } from "@concertable/web-b2b/features/tenant";

function RootLayout() {
  useSyncTenantIdentity();
  return (
    <>
      <Outlet />
      <Toaster richColors />
      {import.meta.env.DEV && <TanStackRouterDevtools />}
    </>
  );
}

export const Route = createRootRoute({
  component: RootLayout,
});
