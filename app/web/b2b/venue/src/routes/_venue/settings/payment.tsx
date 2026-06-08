import { createFileRoute } from "@tanstack/react-router";
import { PaymentPage } from "@/features/payments";
import { PayoutAccountSection } from "@b2b/features/payments";

export const Route = createFileRoute("/_venue/settings/payment")({
  component: () => <PaymentPage payoutSlot={<PayoutAccountSection />} />,
});
