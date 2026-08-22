import { createFileRoute } from "@tanstack/react-router";
import { ModerationPage } from "../../features/moderation";

export const Route = createFileRoute("/_admin/moderation")({
  component: ModerationPage,
});
