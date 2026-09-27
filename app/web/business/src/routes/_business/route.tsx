import { createFileRoute } from "@tanstack/react-router";
import { AppLayout } from "@concertable/web/components/AppLayout";
import { Mailbox } from "@concertable/web-b2b/features/conversations";
import {
  TenantChooser,
  TenantSwitcher,
  resolveTenantRoute,
  useTenant,
} from "@concertable/web-b2b/features/tenant";

const links = [
  { label: "Operations", to: "/app" },
  { label: "Settings", to: "/settings" },
];

function BusinessLayout() {
  const { selectionRequired, session, permissions } = useTenant();
  if (selectionRequired) return <TenantChooser />;
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
      profileItems={links}
      headerSlot={
        <>
          <TenantSwitcher />
          {mailbox}
        </>
      }
    />
  );
}

export const Route = createFileRoute("/_business")({
  beforeLoad: async () => {
    await resolveTenantRoute();
  },
  component: BusinessLayout,
});
