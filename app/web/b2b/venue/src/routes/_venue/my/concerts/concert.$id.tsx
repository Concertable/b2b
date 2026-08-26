import { createFileRoute } from "@tanstack/react-router";
import { MyConcertPage } from "@concertable/web-b2b/features/concerts";
import {
  CancelBookingButton,
  DeclareDoorRevenueButton,
} from "../../../../features/concerts";

export const Route = createFileRoute("/_venue/my/concerts/concert/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: () => {
    const { id } = Route.useParams();
    return (
      <MyConcertPage
        id={id}
        renderActions={(concert) => (
          <>
            {concert.actions?.declareDoorRevenue && (
              <DeclareDoorRevenueButton concert={concert} />
            )}
            {concert.actions?.cancel && (
              <CancelBookingButton concertId={concert.id} />
            )}
          </>
        )}
      />
    );
  },
});
