import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import {
  serializeSearch,
  deserializeSearch,
} from "shared/features/search";
import { APIProvider as MapsProvider } from "@vis.gl/react-google-maps";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "react-oidc-context";
import { userManager, onSigninCallback } from "shared/features/auth";
import { queryClient } from "shared/lib/queryClient";
import { routeTree } from "./routeTree.gen";
import { ThemeProvider } from "shared/providers/ThemeProvider";
import { TooltipProvider } from "shared/components/ui/tooltip";
import "@b2b/lib/b2bAxios";
import "shared/lib/searchAxios";
import "shared/lib/geocoding";
import "shared/index.css";

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
