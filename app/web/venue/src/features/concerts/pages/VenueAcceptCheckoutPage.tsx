import { useEffect, useState } from "react";
import { useParams, useRouter } from "@tanstack/react-router";
import dayjs from "dayjs";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { ConcertDraftCreatedPayload } from "@/features/notifications";
import {
  AcceptContractSummary,
  useAcceptApplicationMutation,
  useAcceptCheckoutQuery,
  useApplicationQuery,
} from "@b2b/features/concerts";
import type { Application, Checkout } from "@/features/concerts";
import { useCheckoutFlow, type CheckoutFlowState } from "@/features/concerts/hooks/useCheckoutFlow";
import { CheckoutLayout } from "@/features/concerts/components/checkout/CheckoutLayout";
import { CheckoutSection } from "@/features/concerts/components/checkout/CheckoutSection";
import { CheckoutEventBanner } from "@/features/concerts/components/checkout/CheckoutEventBanner";
import { OrderSummaryCard } from "@/features/concerts/components/checkout/OrderSummaryCard";
import { CheckoutSuccess } from "@/features/concerts/components/checkout/CheckoutSuccess";
import { CheckoutFlow } from "@/features/concerts/components/checkout/CheckoutFlow";
import { StripePaymentForm } from "@/features/concerts/components/checkout/StripePaymentForm";
import { summaryFor } from "@b2b/features/concerts/utils/acceptCheckoutFormat";

export function VenueAcceptCheckoutPage() {
  const { applicationId } = useParams({ strict: false }) as {
    applicationId: number;
  };
  const {
    data: application,
    isLoading,
    isError,
  } = useApplicationQuery(applicationId);
  const {
    data: checkout,
    isLoading: isCheckoutLoading,
    isError: isCheckoutError,
  } = useAcceptCheckoutQuery(applicationId);

  if (isLoading || isCheckoutLoading) return <CheckoutSkeleton />;
  if (isError || !application)
    return <div className="text-destructive p-6">Application not found.</div>;
  if (isCheckoutError || !checkout)
    return (
      <div className="text-destructive p-6">Could not start checkout.</div>
    );

  return (
    <VenueAcceptCheckoutForm
      applicationId={applicationId}
      application={application}
      checkout={checkout}
    />
  );
}

interface Props {
  artistName: string;
  flow: CheckoutFlowState<ConcertDraftCreatedPayload>;
}

export function VenueAcceptCheckoutFlow({ artistName, flow }: Readonly<Props>) {
  const router = useRouter();

  useEffect(() => {
    if (flow.phase === "success")
      void router.navigate({ to: "/my/concerts/concert/$id", params: { id: flow.result } });
  }, [flow, router]);

  const config = {
    title: "Finalising acceptance",
    timeoutTitle: "Still finalising",
    pendingHint: "Your concert will appear in your dashboard",
    steps: { first: "Acceptance confirmed", final: "Creating concert draft" },
  };

  return (
    <CheckoutFlow
      flow={flow}
      {...config}
      renderSuccess={(concertId) => (
        <VenueAcceptCheckoutSuccess
          artistName={artistName}
          onView={() =>
            void router.navigate({
              to: "/my/concerts/concert/$id",
              params: { id: concertId },
            })
          }
        />
      )}
    />
  );
}

interface VenueAcceptCheckoutFormProps {
  applicationId: number;
  application: Application;
  checkout: Checkout;
}

function VenueAcceptCheckoutForm({ applicationId, application, checkout }: Readonly<VenueAcceptCheckoutFormProps>) {
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const acceptMutation = useAcceptApplicationMutation(application.opportunity.id);
  const flow = useCheckoutFlow<ConcertDraftCreatedPayload>({ event: "ConcertDraftCreated" });

  const { artist, opportunity } = application;
  const { labels } = checkout;

  if (submitted)
    return <VenueAcceptCheckoutFlow artistName={artist.name} flow={flow} />;

  const summary = summaryFor(checkout.amount);

  async function handleAccept(paymentMethodId: string) {
    setError(null);
    try {
      await acceptMutation.mutateAsync({ applicationId, body: { paymentMethodId } });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Acceptance failed. Please try again.");
    }
  }

  return (
    <CheckoutLayout
      banner={
        <CheckoutEventBanner
          title={artist.name}
          subtitle={`${dayjs(opportunity.startDate).format("D MMM YYYY")} – ${dayjs(opportunity.endDate).format("D MMM YYYY")}`}
          meta={`Paying ${checkout.payee.name}${checkout.payee.email ? ` · ${checkout.payee.email}` : ""}`}
        />
      }
      summary={
        <OrderSummaryCard
          title={labels.summaryTitle}
          lines={summary.lines}
          total={summary.total}
        />
      }
    >
      <CheckoutSection title="Contract Terms">
        <AcceptContractSummary contract={opportunity.contract} />
      </CheckoutSection>

      <CheckoutSection
        title="Payment Method"
        description={labels.paymentHint ?? undefined}
      >
        <StripePaymentForm
          session={checkout.session}
          submitLabel={labels.submitLabel}
          disabled={acceptMutation.isPending}
          onSuccess={handleAccept}
        />
      </CheckoutSection>
      {error && <p data-testid="payment-error" className="text-destructive text-sm">{error}</p>}
    </CheckoutLayout>
  );
}

interface VenueAcceptCheckoutSuccessProps {
  artistName: string;
  onView: () => void;
}

function VenueAcceptCheckoutSuccess({ artistName, onView }: Readonly<VenueAcceptCheckoutSuccessProps>) {
  return (
    <CheckoutSuccess
      title="Application Accepted"
      description={
        <>
          You have accepted{" "}
          <span className="text-foreground font-medium">{artistName}</span>.
          Your concert draft is ready.
        </>
      }
      footer={
        <Button onClick={onView} className="mt-2">
          View concert
        </Button>
      }
    />
  );
}

function CheckoutSkeleton() {
  return (
    <div className="mx-auto max-w-5xl space-y-6 px-4 py-8 lg:py-12">
      <Skeleton className="h-9 w-40" />
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_380px] lg:gap-8">
        <div className="space-y-6">
          <Skeleton className="h-28 w-full rounded-xl" />
          <Skeleton className="h-40 w-full rounded-xl" />
          <Skeleton className="h-44 w-full rounded-xl" />
        </div>
        <Skeleton className="h-60 w-full rounded-xl" />
      </div>
    </div>
  );
}
