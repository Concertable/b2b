import { createRootRoute, Outlet } from "@tanstack/react-router";
import { Toaster } from "@concertable/web/components/ui/sonner";
import { CookieConsentBanner } from "@concertable/web/components/CookieConsentBanner";
import { useTenantIdentity } from "@concertable/web-b2b/features/tenant";

function RootLayout() {
  useTenantIdentity();
  return (
    <>
      <Outlet />
      <Toaster richColors />
      <CookieConsentBanner />
    </>
  );
}

export const Route = createRootRoute({ component: RootLayout });
