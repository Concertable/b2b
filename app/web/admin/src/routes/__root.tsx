import { createRootRoute, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/router-devtools";
import { Toaster } from "@concertable/web/components/ui/sonner";

function RootLayout() {
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
