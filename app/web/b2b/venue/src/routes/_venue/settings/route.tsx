import { createFileRoute } from "@tanstack/react-router";
import { SettingsLayout } from "@/components/SettingsLayout";

export const Route = createFileRoute("/_venue/settings")({
  component: SettingsLayout,
});
