import { createFileRoute } from "@tanstack/react-router";
import {
  TenantChooser,
  TenantSwitcher,
  resolveTenantRoute,
  useTenant,
} from "@concertable/b2b/web/shared/features/tenant";
import { useArtistNotifications } from "../../features/notifications";
import { requireArtist } from "../../features/artist";
import { AppLayout } from "@concertable/web/shared/components/AppLayout";
import type { ProfileMenuItem } from "@concertable/web/shared/components/ProfileMenu";

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
  const { selectionRequired } = useTenant("Artist");
  if (selectionRequired) return <TenantChooser tenantType="Artist" />;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={<TenantSwitcher tenantType="Artist" />}
    />
  );
}

export const Route = createFileRoute("/_artist")({
  beforeLoad: async ({ location }) => {
    const { selectionRequired } = await resolveTenantRoute("Artist");
    if (selectionRequired) return;
    await requireArtist({ pathname: location.pathname });
  },
  component: ArtistLayout,
});
