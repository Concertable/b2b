import { useArtistOverviewQuery } from "./hooks";
import { StripeConnectBanner } from "@concertable/web/shared/features/dashboard";

export function ArtistStripeBanner() {
  const { data } = useArtistOverviewQuery();
  if (!data) return null;
  return <StripeConnectBanner status={data.stripeConnect} />;
}
