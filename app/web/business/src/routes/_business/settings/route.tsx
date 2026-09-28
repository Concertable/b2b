import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@concertable/web/components/SettingsLayout";

const extraLinks = [
  { label: "Organization", to: "/settings/organization" },
  { label: "Members", to: "/settings/members" },
];

export const Route = createFileRoute("/_business/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
