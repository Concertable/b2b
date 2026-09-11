import { createFileRoute } from "@tanstack/react-router";
import { CreateVenuePage } from "../../features/venue";

export const Route = createFileRoute("/_venue/create")({
  component: CreateVenuePage,
});
