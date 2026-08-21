import { createFileRoute, Link, Outlet } from "@tanstack/react-router";
import { useAuth } from "react-oidc-context";
import { Button } from "@concertable/web/components/ui/button";
import { requireAdmin } from "../../features/identity";

function AdminLayout() {
  const auth = useAuth();
  const email = auth.user?.profile.email;

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-border flex items-center justify-between border-b px-6 py-3">
        <div className="flex items-center gap-6">
          <span className="font-semibold">Concertable Admin</span>
          <nav className="flex items-center gap-4 text-sm">
            <Link
              to="/"
              className="text-muted-foreground hover:text-foreground [&.active]:text-foreground [&.active]:font-medium"
            >
              Admins
            </Link>
            <Link
              to="/moderation"
              className="text-muted-foreground hover:text-foreground [&.active]:text-foreground [&.active]:font-medium"
            >
              Moderation
            </Link>
          </nav>
        </div>
        <div className="flex items-center gap-3">
          {email && <span className="text-muted-foreground text-sm">{email}</span>}
          <Button variant="ghost" size="sm" onClick={() => auth.signoutRedirect()}>
            Sign out
          </Button>
        </div>
      </header>
      <main className="flex flex-1 flex-col p-6">
        <Outlet />
      </main>
    </div>
  );
}

export const Route = createFileRoute("/_admin")({
  beforeLoad: async ({ location }) => {
    await requireAdmin({ location });
  },
  component: AdminLayout,
});
