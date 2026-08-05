import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@/components/SettingsLayout";

const extraLinks = [
  { label: "Organization", to: "/settings/organization" },
  { label: "Self-billing agreement", to: "/settings/self-billing-agreement" },
  { label: "Members", to: "/settings/members" },
];

export const Route = createFileRoute("/_venue/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
