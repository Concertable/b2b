import { createFileRoute } from "@tanstack/react-router";
import { SettingsPage } from "@concertable/web/shared/features/user";

export const Route = createFileRoute("/_artist/settings/")({
  component: SettingsPage,
});
