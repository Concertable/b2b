import { useEffect, useRef } from "react";
import { useAuth } from "react-oidc-context";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { z } from "zod";

function LoginRedirect() {
  const auth = useAuth();
  const navigate = useNavigate();
  const { redirect } = Route.useSearch();
  const redirectStarted = useRef(false);

  useEffect(() => {
    if (auth.isLoading || auth.activeNavigator) return;
    if (auth.isAuthenticated) {
      void navigate({ to: redirect ?? "/app", replace: true });
      return;
    }
    if (redirectStarted.current) return;
    redirectStarted.current = true;
    void auth.signinRedirect({ state: { redirect } });
  }, [auth, auth.activeNavigator, auth.isAuthenticated, auth.isLoading, navigate, redirect]);
  return null;
}

export const Route = createFileRoute("/login")({
  validateSearch: z.object({ redirect: z.string().optional() }),
  component: LoginRedirect,
});
