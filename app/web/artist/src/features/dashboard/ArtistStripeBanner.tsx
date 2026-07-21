import { useArtistOverviewQuery } from "./hooks";
import { StripeConnectBanner } from "@/features/dashboard";

export function ArtistStripeBanner() {
  const { data } = useArtistOverviewQuery();
  if (!data) return null;
  return <StripeConnectBanner status={data.stripeConnect} />;
}
