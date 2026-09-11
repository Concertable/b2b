import { Link } from "@tanstack/react-router";
import { Button } from "@concertable/web/components/ui/button";
import { useVerificationQuery } from "../hooks/useVerificationQuery";

export function VerificationBanner() {
  const { data: verification, isLoading } = useVerificationQuery();

  // Approved (or still loading) needs no nag. No row = never submitted.
  if (isLoading || verification?.status === "approved") return null;

  const isRejected = verification?.status === "rejected";
  const isPending = verification?.status === "pending";

  return (
    <div className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4">
      <div className="space-y-0.5">
        <p className="font-medium">
          {isRejected
            ? "Your verification needs new evidence"
            : isPending
              ? "Your verification is being reviewed"
              : "Verify your organisation to publish opportunities and get paid"}
        </p>
        <p className="text-muted-foreground text-sm">
          {isRejected
            ? verification?.rejectionReason
              ? `We couldn't approve your last submission: ${verification.rejectionReason}`
              : "We couldn't approve your last submission. Upload new evidence to try again."
            : isPending
              ? "We'll email you once it's been checked. You can't publish opportunities or be paid out until it's approved."
              : "We need to confirm your organisation is legitimate before you can publish opportunities or be paid out."}
        </p>
      </div>
      {!isPending && (
        <Button size="sm" asChild>
          <Link to="/settings/verification">
            {isRejected ? "Upload new evidence" : "Start verification"}
          </Link>
        </Button>
      )}
    </div>
  );
}
