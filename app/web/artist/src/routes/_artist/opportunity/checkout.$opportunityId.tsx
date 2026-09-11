import { createFileRoute } from "@tanstack/react-router";
import { ArtistApplyCheckoutPage } from "../../../features/concerts";

export const Route = createFileRoute(
  "/_artist/opportunity/checkout/$opportunityId",
)({
  params: {
    parse: (params) => ({ opportunityId: Number(params.opportunityId) }),
    stringify: (params) => ({ opportunityId: String(params.opportunityId) }),
  },
  component: ArtistApplyCheckoutPage,
});
