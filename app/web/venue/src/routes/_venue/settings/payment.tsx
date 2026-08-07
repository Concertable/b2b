import { createFileRoute } from "@tanstack/react-router";
import { PaymentPage } from "@concertable/web/features/payments";
import { PayoutAccountSection } from "@concertable/b2b/features/payments";

export const Route = createFileRoute("/_venue/settings/payment")({
  component: () => <PaymentPage payoutSlot={<PayoutAccountSection />} />,
});
