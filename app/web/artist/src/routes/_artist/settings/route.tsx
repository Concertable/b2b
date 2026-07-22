import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@/components/SettingsLayout";

const extraLinks = [
  { label: "Business & tax details", to: "/settings/organization" },
  { label: "Members", to: "/settings/members" },
];

export const Route = createFileRoute("/_artist/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
