import { createFileRoute } from "@tanstack/react-router";
import { requireBusinessRole } from "@/features/auth";
import { useArtistNotifications } from "../../features/notifications";
import { requireArtist } from "../../features/artist";
import { AppLayout } from "@/components/AppLayout";
import type { ProfileMenuItem } from "@/components/ProfileMenu";

const links = [
  { label: "Dashboard", to: "/" },
  { label: "My Concerts", to: "/my" },
  { label: "Find Venues", to: "/find" },
];

const profileItems: ProfileMenuItem[] = [
  { label: "My Artist", to: "/my" },
  { label: "Dashboard", to: "/" },
];

function ArtistLayout() {
  useArtistNotifications();
  return <AppLayout links={links} profileItems={profileItems} />;
}

export const Route = createFileRoute("/_artist")({
  beforeLoad: async ({ location }) => {
    await requireBusinessRole("ArtistManager");
    await requireArtist({ pathname: location.pathname });
  },
  component: ArtistLayout,
});
