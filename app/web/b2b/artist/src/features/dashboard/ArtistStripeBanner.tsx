import { useArtistOverview } from "./hooks";
import { StripeConnectBanner } from "@/features/dashboard";

export function ArtistStripeBanner() {
  const { data } = useArtistOverview();
  if (!data) return null;
  return <StripeConnectBanner status={data.stripeConnect} />;
}
