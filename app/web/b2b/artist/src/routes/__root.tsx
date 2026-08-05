import { createRootRoute, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/router-devtools";
import { Toaster } from "@concertable/web/shared/components/ui/sonner";
import { useTenantIdentity } from "@concertable/b2b/web/shared/features/tenant";

function RootLayout() {
  useTenantIdentity();
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
