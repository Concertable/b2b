import { createFileRoute } from "@tanstack/react-router";
import { requireBusinessRole } from "@/features/auth";
import {
  TenantChooser,
  TenantSwitcher,
  getTenantChoicePending,
  reconcileActiveTenant,
  useTenantChoicePending,
} from "@b2b/features/tenant";
import { useArtistNotifications } from "../../features/notifications";
import { requireArtist } from "../../features/artist";
import { AppLayout } from "@/components/AppLayout";
import type { ProfileMenuItem } from "@/components/ProfileMenu";

const links = [
  { label: "Dashboard", to: "/" },
  { label: "My Concerts", to: "/my" },
  { label: "My Applications", to: "/my/applications" },
  { label: "Find Venues", to: "/find" },
];

const profileItems: ProfileMenuItem[] = [
  { label: "My Artist", to: "/my" },
  { label: "Dashboard", to: "/" },
];

function ArtistLayout() {
  useArtistNotifications();
  if (useTenantChoicePending("Artist")) return <TenantChooser persona="Artist" />;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={<TenantSwitcher persona="Artist" />}
    />
  );
}

export const Route = createFileRoute("/_artist")({
  beforeLoad: async ({ location }) => {
    await requireBusinessRole("ArtistManager");
    reconcileActiveTenant("Artist");
    if (getTenantChoicePending("Artist")) return;
    await requireArtist({ pathname: location.pathname });
  },
  component: ArtistLayout,
});
