import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { useAuth } from "react-oidc-context";
import { Loader2 } from "lucide-react";

function AuthCallback() {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!auth.isAuthenticated) return;
    const redirect = (auth.user?.state as { redirect?: string } | undefined)?.redirect;
    void navigate({ to: redirect ?? "/" });
  }, [auth.isAuthenticated, auth.user, navigate]);

  if (auth.error)
    return (
      <div className="flex min-h-screen items-center justify-center p-8">
        Sign-in failed: {auth.error.message}
      </div>
    );

  return (
    <div className="flex min-h-screen items-center justify-center">
      <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
    </div>
  );
}

export const Route = createFileRoute("/auth/callback")({
  component: AuthCallback,
});
