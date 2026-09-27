import { createFileRoute } from "@tanstack/react-router";
import {
  TenantChooser,
  TenantSwitcher,
  requireLocalB2bAuth,
  resolveTenantRoute,
  useTenant,
} from "@concertable/web-b2b/features/tenant";
import { useArtistNotifications } from "../../features/notifications";
import { requireArtist } from "../../features/artist";
import { AppLayout } from "@concertable/web/components/AppLayout";
import type { ProfileMenuItem } from "@concertable/web/components/ProfileMenu";
import { Mailbox } from "@concertable/web-b2b/features/conversations";

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
  const { selectionRequired, session, permissions } = useTenant("artist");
  useArtistNotifications(session);
  if (selectionRequired) return <TenantChooser businessActivity="artist" />;
  const mailbox =
    session !== undefined && permissions.has("messages.read") ? (
      <Mailbox
        key={`${session.tenantId}:${session.membershipId}:${session.permissionVersion}:${session.generation}`}
        session={session}
      />
    ) : undefined;
  return (
    <AppLayout
      links={links}
      profileItems={profileItems}
      headerSlot={
        <>
          <TenantSwitcher businessActivity="artist" />
          {mailbox}
        </>
      }
    />
  );
}

export const Route = createFileRoute("/_artist")({
  beforeLoad: async ({ location }) => {
    if (location.pathname === "/create") {
      await requireLocalB2bAuth({ location });
      return;
    }
    const { selectionRequired } = await resolveTenantRoute("artist");
    if (selectionRequired) return;
    await requireArtist({ pathname: location.pathname });
  },
  component: ArtistLayout,
});
