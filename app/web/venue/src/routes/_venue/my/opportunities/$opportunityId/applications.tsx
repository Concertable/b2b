import { createFileRoute } from "@tanstack/react-router";
import { ApplicationsPage } from "../../../../../features/concerts";

export const Route = createFileRoute("/_venue/my/opportunities/$opportunityId/applications")({
  params: {
    parse: (params) => ({ opportunityId: Number(params.opportunityId) }),
    stringify: (params) => ({ opportunityId: String(params.opportunityId) }),
  },
  component: ApplicationsPage,
});
