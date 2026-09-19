import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { AuthProvider } from "react-oidc-context";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import { userManager, onSigninCallback } from "@concertable/web/features/auth";
import { queryClient } from "@concertable/web/lib/queryClient";
import { ConsentProvider } from "@concertable/web/providers/ConsentProvider";
import { ThemeProvider } from "@concertable/web/providers/ThemeProvider";
import { TooltipProvider } from "@concertable/web/components/ui/tooltip";
import { serializeSearch, deserializeSearch } from "@concertable/web/features/search";
import { routeTree } from "./routeTree.gen";
import "@concertable/web-b2b/lib/b2bClient";
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
        <ThemeProvider>
          <ConsentProvider>
            <TooltipProvider>
              <RouterProvider router={router} />
            </TooltipProvider>
          </ConsentProvider>
        </ThemeProvider>
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
);
