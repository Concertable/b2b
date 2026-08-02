import { createRootRoute, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/router-devtools";
import { Toaster } from "@/components/ui/sonner";
import { useSyncB2bIdentity } from "@b2b/features/tenant";

function RootLayout() {
  useSyncB2bIdentity();
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
