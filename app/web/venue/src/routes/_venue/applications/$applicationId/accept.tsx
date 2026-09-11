import { createFileRoute } from "@tanstack/react-router";
import { AcceptApplicationPage } from "../../../../features/concerts";

export const Route = createFileRoute("/_venue/applications/$applicationId/accept")({
  params: {
    parse: (params) => ({ applicationId: Number(params.applicationId) }),
    stringify: (params) => ({ applicationId: String(params.applicationId) }),
  },
  component: AcceptApplicationPage,
});
