import { createFileRoute } from "@tanstack/react-router";
import { PaymentPage } from "@concertable/web/features/payments";
import { PayoutAccountSection } from "@concertable/web-b2b/features/payments";

export const Route = createFileRoute("/_artist/settings/payment")({
  component: () => <PaymentPage payoutSlot={<PayoutAccountSection />} />,
});
