import { useEffect } from "react";
import { useAuth } from "react-oidc-context";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Loader2 } from "lucide-react";

function AuthCallback() {
  const auth = useAuth();
  const navigate = useNavigate();
  useEffect(() => {
    if (!auth.isAuthenticated) return;
    const redirect = (auth.user?.state as { redirect?: string } | undefined)
      ?.redirect;
    void navigate({ to: redirect ?? "/app" });
  }, [auth.isAuthenticated, auth.user, navigate]);
  if (auth.error)
    return <div className="p-8">Sign-in failed: {auth.error.message}</div>;
  return (
    <div className="flex min-h-screen items-center justify-center">
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
    </div>
  );
}

export const Route = createFileRoute("/auth/callback")({ component: AuthCallback });
