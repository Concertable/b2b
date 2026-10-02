import { createFileRoute } from "@tanstack/react-router";
import { AppLayout } from "@concertable/web/components/AppLayout";
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
  const { selectionRequired } = useTenant();
  if (selectionRequired) return <TenantChooser />;
  return (
    <AppLayout
      links={links}
      profileItems={links}
      headerSlot={<TenantSwitcher />}
    />
  );
}

export const Route = createFileRoute("/_business")({
  beforeLoad: async () => {
    await resolveTenantRoute();
  },
  component: BusinessLayout,
});
