import { PersonaSwitcher, SectionGrid } from "@/features/dashboard";
import { ArtistActivityWidget } from "./ArtistActivityWidget";
import { ArtistApplicationsPipelineWidget } from "./ArtistApplicationsPipelineWidget";
import { ArtistInboxWidget } from "./ArtistInboxWidget";
import { ArtistKpiStrip } from "./ArtistKpiStrip";
import { ArtistNextConcertHero } from "./ArtistNextConcertHero";
import { ArtistPayoutChartWidget } from "./ArtistPayoutChartWidget";
import { ArtistRecommendedOpportunitiesStrip } from "./ArtistRecommendedOpportunitiesStrip";
import { ArtistReviewsWidget } from "./ArtistReviewsWidget";
import { ArtistStripeBanner } from "./ArtistStripeBanner";
import { ArtistWelcomeRow } from "./ArtistWelcomeRow";

export function ArtistDashboardPage() {
  return (
    <div className="flex w-full flex-col gap-4 p-4 md:p-6">
      <SectionGrid>
        <ArtistWelcomeRow />
      </SectionGrid>

      <SectionGrid>
        <ArtistKpiStrip />
      </SectionGrid>

      <ArtistStripeBanner />

      <ArtistNextConcertHero />

      <SectionGrid>
        <div className="col-span-12 lg:col-span-7">
          <ArtistApplicationsPipelineWidget />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <ArtistInboxWidget />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12 lg:col-span-7">
          <ArtistPayoutChartWidget />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <ArtistReviewsWidget />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12">
          <ArtistRecommendedOpportunitiesStrip />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12">
          <ArtistActivityWidget />
        </div>
      </SectionGrid>

      <PersonaSwitcher />
    </div>
  );
}
