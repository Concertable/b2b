import { createFileRoute, Outlet } from "@tanstack/react-router";
import { useAuth } from "react-oidc-context";
import { Button } from "@concertable/web/components/ui/button";
import { Navbar, type NavLink } from "@concertable/web/components/Navbar";
import { requireAdmin } from "../../features/identity";

const links: NavLink[] = [
  { label: "Admins", to: "/" },
  { label: "Moderation", to: "/moderation" },
  { label: "Venues", to: "/venues" },
];

function AdminProfileSlot() {
  const auth = useAuth();
  const email = auth.user?.profile.email;

  return (
    <div className="flex items-center gap-3">
      {email && <span className="text-primary-foreground/70 text-sm">{email}</span>}
      <Button variant="ghost" size="sm" onClick={() => auth.signoutRedirect()}>
        Sign out
      </Button>
    </div>
  );
}

function AdminLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar
        links={links}
        showSearch={false}
        showMailbox={false}
        profileSlot={<AdminProfileSlot />}
        onHeightChange={() => {}}
      />
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
