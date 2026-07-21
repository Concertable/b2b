import { useVenueOverviewQuery } from "./hooks";
import { StripeConnectBanner } from "@/features/dashboard";

export function VenueStripeBanner() {
  const { data } = useVenueOverviewQuery();
  if (!data) return null;
  return <StripeConnectBanner status={data.stripeConnect} />;
}
