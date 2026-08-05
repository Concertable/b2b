import { createFileRoute } from "@tanstack/react-router";
import { PaymentPage } from "@concertable/web/shared/features/payments";
import { PayoutAccountSection } from "@concertable/b2b/web/shared/features/payments";

export const Route = createFileRoute("/_artist/settings/payment")({
  component: () => <PaymentPage payoutSlot={<PayoutAccountSection />} />,
});
