import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@/components/SettingsLayout";

const extraLinks = [{ label: "Organization", to: "/settings/organization" }];

export const Route = createFileRoute("/_venue/settings")({
  component: () => <SettingsLayout extraLinks={extraLinks} />,
});
