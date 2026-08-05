import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import {
  serializeSearch,
  deserializeSearch,
} from "@concertable/web/features/search";
import { APIProvider as MapsProvider } from "@vis.gl/react-google-maps";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "react-oidc-context";
import { userManager, onSigninCallback } from "@concertable/web/features/auth";
import { queryClient } from "@concertable/web/lib/queryClient";
import { routeTree } from "./routeTree.gen";
import { ThemeProvider } from "@concertable/web/providers/ThemeProvider";
import { TooltipProvider } from "@concertable/web/components/ui/tooltip";
import { ConsentProvider } from "@concertable/web/providers/ConsentProvider";
import { CookieConsentBanner } from "@concertable/web/components/CookieConsentBanner";
import "@concertable/b2b/lib/b2bClient";
import "@concertable/web/lib/searchClient";
import "@concertable/web/lib/geocoding";
import "@concertable/web/index.css";

const router = createRouter({
  routeTree,
  stringifySearch: serializeSearch,
  parseSearch: deserializeSearch,
  defaultStructuralSharing: true,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AuthProvider userManager={userManager} onSigninCallback={onSigninCallback}>
      <QueryClientProvider client={queryClient}>
        <MapsProvider
          apiKey={import.meta.env.VITE_GOOGLE_MAPS_API_KEY}
          libraries={["places"]}
        >
          <ThemeProvider>
            <ConsentProvider>
              <TooltipProvider>
                <RouterProvider router={router} />
              </TooltipProvider>
              <CookieConsentBanner />
            </ConsentProvider>
          </ThemeProvider>
        </MapsProvider>
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
);
