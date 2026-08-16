import { useState } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import { usePayoutAccountStatusQuery, StripeOnboardingBanner } from "@concertable/b2b/features/payments";
import { Button } from "@concertable/web/components/ui/button";
import dayjs from "dayjs";
import {
  useApplicationQuery,
  useAcceptApplicationMutation,
  AcceptDealSummary,
  ESignaturePanel,
  useESignature,
} from "@concertable/b2b/features/concerts";
import { VenueAcceptCheckoutFlow } from "./VenueAcceptCheckoutPage";

export function AcceptApplicationPage() {
  const { applicationId } = useParams({ from: "/_venue/applications/$applicationId/accept" });
  const navigate = useNavigate();
  const [accepted, setAccepted] = useState(false);
  const { signature, setSignature, isValid } = useESignature();
  const { data: application, isLoading } = useApplicationQuery(applicationId);
  const { data: accountStatus } = usePayoutAccountStatusQuery(true);
  const acceptMutation = useAcceptApplicationMutation(
    application?.opportunity.id ?? 0,
  );

  if (isLoading || !application) return null;

  const { artist, opportunity, actions } = application;
  const requiresCheckout = actions.checkout != null;

  if (accepted)
    return <VenueAcceptCheckoutFlow applicationId={applicationId} />;

  function handleConfirm() {
    if (requiresCheckout) {
      void navigate({
        to: "/applications/$applicationId/checkout",
        params: { applicationId },
      });
      return;
    }

    acceptMutation.mutate(
      { applicationId, eSignature: signature },
      { onSuccess: () => setAccepted(true) },
    );
  }

  return (
    <div className="mx-auto max-w-lg space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">Review &amp; Accept</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          {artist.name} · {dayjs(opportunity.startDate).format("D MMM YYYY")} —{" "}
          {dayjs(opportunity.endDate).format("D MMM YYYY")}
        </p>
      </div>

      <StripeOnboardingBanner />

      {requiresCheckout ? (
        <div className="border-border bg-card rounded-xl border p-4">
          <AcceptDealSummary deal={opportunity.deal} />
        </div>
      ) : (
        <ESignaturePanel
          deal={opportunity.deal}
          value={signature}
          onChange={setSignature}
        />
      )}

      <div className="flex gap-3">
        <Button
          variant="outline"
          onClick={() =>
            navigate({
              to: "/my/opportunities/$opportunityId/applications",
              params: { opportunityId: opportunity.id },
            })
          }
        >
          Cancel
        </Button>
        <Button
          disabled={accountStatus !== "verified" || acceptMutation.isPending || (!requiresCheckout && !isValid)}
          onClick={handleConfirm}
          data-testid="confirm"
        >
          {requiresCheckout ? "Continue" : "Accept"}
        </Button>
      </div>
    </div>
  );
}
