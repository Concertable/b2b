import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@concertable/web/shared/components/SettingsLayout";

const extraLinks = [
  { label: "Business & tax details", to: "/settings/organization" },
  { label: "Self-billing agreement", to: "/settings/self-billing-agreement" },
  { label: "Members", to: "/settings/members" },
];

export const Route = createFileRoute("/_artist/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
