import { createFileRoute } from "@tanstack/react-router";
import { SettingsPage } from "@/features/user";

export const Route = createFileRoute("/_venue/settings/")({
  component: SettingsPage,
});
