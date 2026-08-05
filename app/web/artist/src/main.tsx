import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import {
  serializeSearch,
  deserializeSearch,
} from "@concertable/web/shared/features/search";
import { APIProvider as MapsProvider } from "@vis.gl/react-google-maps";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "react-oidc-context";
import { userManager, onSigninCallback } from "@concertable/web/shared/features/auth";
import { queryClient } from "@concertable/web/shared/lib/queryClient";
import { routeTree } from "./routeTree.gen";
import { ThemeProvider } from "@concertable/web/shared/providers/ThemeProvider";
import { TooltipProvider } from "@concertable/web/shared/components/ui/tooltip";
import "@concertable/b2b/web/shared/lib/b2bClient";
import "@concertable/web/shared/lib/searchClient";
import "@concertable/web/shared/lib/geocoding";
import "@concertable/web/shared/index.css";

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
            <TooltipProvider>
              <RouterProvider router={router} />
            </TooltipProvider>
          </ThemeProvider>
        </MapsProvider>
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
);
