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
import { Mailbox } from "@concertable/web/features/messaging";

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
  const { selectionRequired, session } = useTenant("venueOperator");
  useVenueNotifications(session);
  if (selectionRequired) return <TenantChooser businessActivity="venueOperator" />;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={<TenantSwitcher businessActivity="venueOperator" />}
      messagingSlot={<Mailbox />}
    />
  );
}

export const Route = createFileRoute("/_venue")({
  beforeLoad: async ({ location }) => {
    if (location.pathname === "/create") {
      await requireLocalB2bAuth({ location });
      return;
    }
    const { selectionRequired } = await resolveTenantRoute("venueOperator");
    if (selectionRequired) return;
    await requireVenue({ pathname: location.pathname });
  },
  component: VenueLayout,
});
