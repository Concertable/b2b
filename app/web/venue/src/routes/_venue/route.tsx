import { createFileRoute } from "@tanstack/react-router";
import {
  TenantChooser,
  TenantSwitcher,
  isTenantChoicePending,
  reconcileActiveTenant,
  requireBusinessPersona,
  useTenantChoicePending,
} from "@b2b/features/tenant";
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
  if (useTenantChoicePending("Venue")) return <TenantChooser persona="Venue" />;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={<TenantSwitcher persona="Venue" />}
    />
  );
}

export const Route = createFileRoute("/_venue")({
  beforeLoad: async ({ location }) => {
    await requireBusinessPersona("Venue");
    reconcileActiveTenant("Venue");
    if (isTenantChoicePending("Venue")) return;
    await requireVenue({ pathname: location.pathname });
  },
  component: VenueLayout,
});
