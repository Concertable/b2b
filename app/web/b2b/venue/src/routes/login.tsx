import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { useAuth } from "react-oidc-context";
import { z } from "zod";

function LoginRedirect() {
  const auth = useAuth();
  const navigate = useNavigate();
  const { redirect } = Route.useSearch();

  useEffect(() => {
    if (auth.isLoading) return;
    if (auth.activeNavigator) return;
    if (auth.isAuthenticated) {
      void navigate({ to: redirect || "/", replace: true });
      return;
    }
    void auth.signinRedirect({ state: { redirect } });
  }, [auth.isLoading, auth.isAuthenticated, auth.activeNavigator, auth, navigate, redirect]);

  return null;
}

export const Route = createFileRoute("/login")({
  validateSearch: z.object({ redirect: z.string().optional() }),
  component: LoginRedirect,
});
