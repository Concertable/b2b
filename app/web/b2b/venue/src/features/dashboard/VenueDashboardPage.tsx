import { PersonaSwitcher, SectionGrid } from "@/features/dashboard";
import { VenueActivityWidget } from "./VenueActivityWidget";
import { VenueApplicationsWidget } from "./VenueApplicationsWidget";
import { VenueInboxWidget } from "./VenueInboxWidget";
import { VenueKpiStrip } from "./VenueKpiStrip";
import { VenueOpenOpportunitiesWidget } from "./VenueOpenOpportunitiesWidget";
import { VenueRevenueChartWidget } from "./VenueRevenueChartWidget";
import { VenueReviewsWidget } from "./VenueReviewsWidget";
import { VenueSettlementsWidget } from "./VenueSettlementsWidget";
import { VenueStripeBanner } from "./VenueStripeBanner";
import { VenueUpcomingConcertsStrip } from "./VenueUpcomingConcertsStrip";
import { VenueWelcomeRow } from "./VenueWelcomeRow";

export function VenueDashboardPage() {
  return (
    <div className="flex w-full flex-col gap-4 p-4 md:p-6">
      <SectionGrid>
        <VenueWelcomeRow />
      </SectionGrid>

      <SectionGrid>
        <VenueKpiStrip />
      </SectionGrid>

      <VenueStripeBanner />

      <SectionGrid>
        <div className="col-span-12 lg:col-span-7">
          <VenueApplicationsWidget />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <VenueInboxWidget />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12">
          <VenueUpcomingConcertsStrip />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12 lg:col-span-7">
          <VenueRevenueChartWidget />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <VenueReviewsWidget />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12 lg:col-span-7">
          <VenueOpenOpportunitiesWidget />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <VenueActivityWidget />
        </div>
      </SectionGrid>

      <SectionGrid>
        <div className="col-span-12">
          <VenueSettlementsWidget />
        </div>
      </SectionGrid>

      <PersonaSwitcher />
    </div>
  );
}
