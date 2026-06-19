import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@/components/SettingsLayout";

const extraLinks = [
  { label: "Business & tax details", to: "/settings/organization" },
];

export const Route = createFileRoute("/_artist/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
