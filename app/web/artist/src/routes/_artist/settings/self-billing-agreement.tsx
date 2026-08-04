import { createFileRoute } from "@tanstack/react-router";
import { SelfBillingAgreementPage } from "@b2b/features/selfBilling";

export const Route = createFileRoute("/_artist/settings/self-billing-agreement")({
  component: SelfBillingAgreementPage,
});
