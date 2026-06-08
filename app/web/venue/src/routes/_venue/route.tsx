import { createFileRoute } from "@tanstack/react-router";
import { requireBusinessRole } from "@/features/auth";
import { useVenueNotifications } from "../../features/notifications";
import { requireVenue } from "../../features/venue";
import { AppLayout } from "@/components/AppLayout";
import type { ProfileMenuItem } from "@/components/ProfileMenu";

const links = [
  { label: "Dashboard", to: "/" },
  { label: "My Venue", to: "/my" },
  { label: "My Concerts", to: "/my/concerts" },
  { label: "Find Artists", to: "/find" },
];

const profileItems: ProfileMenuItem[] = [
  { label: "My Venue", to: "/my" },
  { label: "Dashboard", to: "/" },
];

function VenueLayout() {
  useVenueNotifications();
  return <AppLayout links={links} profileItems={profileItems} />;
}

export const Route = createFileRoute("/_venue")({
  beforeLoad: async ({ location }) => {
    await requireBusinessRole("VenueManager");
    await requireVenue({ pathname: location.pathname });
  },
  component: VenueLayout,
});
