import { createFileRoute } from "@tanstack/react-router";
import { MyVenuePage } from "../../../features/venue";

export const Route = createFileRoute("/_venue/my/")({
  component: MyVenuePage,
});
