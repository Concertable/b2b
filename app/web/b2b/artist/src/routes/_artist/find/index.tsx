import { createFileRoute } from "@tanstack/react-router";
import { FindVenuePage } from "../../../features/search";
import { SearchSchema } from "@/features/search";

export const Route = createFileRoute("/_artist/find/")({
  component: FindVenuePage,
  validateSearch: SearchSchema(),
});
