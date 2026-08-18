import { createFileRoute } from "@tanstack/react-router";
import {
  TenantChooser,
  TenantSwitcher,
  requireLocalB2bAuth,
  resolveTenantRoute,
  useTenant,
} from "@concertable/web-b2b/features/tenant";
import { useVenueNotifications } from "../../features/notifications";
import { requireVenue } from "../../features/venue";
import { AppLayout } from "@concertable/web/components/AppLayout";
import type { ProfileMenuItem } from "@concertable/web/components/ProfileMenu";

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
  const { selectionRequired } = useTenant("Venue");
  if (selectionRequired) return <TenantChooser tenantType="Venue" />;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={<TenantSwitcher tenantType="Venue" />}
    />
  );
}

export const Route = createFileRoute("/_venue")({
  beforeLoad: async ({ location }) => {
    if (location.pathname === "/create") {
      await requireLocalB2bAuth({ location });
      return;
    }
    const { selectionRequired } = await resolveTenantRoute("Venue");
    if (selectionRequired) return;
    await requireVenue({ pathname: location.pathname });
  },
  component: VenueLayout,
});
