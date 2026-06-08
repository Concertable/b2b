import { useVenueOverview } from "./hooks";
import { StripeConnectBanner } from "@/features/dashboard";

export function VenueStripeBanner() {
  const { data } = useVenueOverview();
  if (!data) return null;
  return <StripeConnectBanner status={data.stripeConnect} />;
}
