import { createFileRoute } from "@tanstack/react-router";
import { VerificationPage } from "../../features/verification";

export const Route = createFileRoute("/_admin/verification")({
  component: VerificationPage,
});
