import { useState } from "react";
import { Navigate, useParams } from "@tanstack/react-router";
import dayjs from "dayjs";
import { Skeleton } from "@concertable/web/components/ui/skeleton";
import {
  AcceptDealSummary,
  ESignaturePanel,
  useAcceptApplicationMutation,
  useAcceptCheckoutQuery,
  useApplicationQuery,
  useESignature,
  useMyConcertByApplicationQuery,
} from "@concertable/b2b/features/concerts";
import type { Application } from "@concertable/b2b/features/concerts";
import type { Checkout } from "@concertable/web/features/concerts";
import { CheckoutLayout } from "@concertable/web/features/concerts/components/checkout/CheckoutLayout";
import { CheckoutSection } from "@concertable/web/features/concerts/components/checkout/CheckoutSection";
import { CheckoutEventBanner } from "@concertable/web/features/concerts/components/checkout/CheckoutEventBanner";
import { OrderSummaryCard } from "@concertable/web/features/concerts/components/checkout/OrderSummaryCard";
import { CheckoutAwaiting } from "@concertable/web/features/concerts/components/checkout/CheckoutAwaiting";
import { StripePaymentForm } from "@concertable/web/features/concerts/components/checkout/StripePaymentForm";
import { summaryFor } from "@concertable/b2b/features/concerts/utils/acceptCheckoutFormat";

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
  applicationId: number;
}

export function VenueAcceptCheckoutFlow({ applicationId }: Readonly<Props>) {
  const { data: concert } = useMyConcertByApplicationQuery(applicationId);

  if (concert)
    return (
      <Navigate
        to="/my/concerts/concert/$id"
        params={{ id: concert.id }}
        replace
      />
    );

  return (
    <CheckoutAwaiting
      title="Finalising acceptance"
      description="This usually takes a few seconds. Please don't close this page."
      steps={[
        { label: "Acceptance confirmed", status: "done" },
        { label: "Confirming with our system", status: "active" },
        { label: "Creating concert draft", status: "pending" },
      ]}
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
  const { signature, setSignature, isValid } = useESignature();
  const [error, setError] = useState<string | null>(null);
  const acceptMutation = useAcceptApplicationMutation(application.opportunity.id);
  const { artist, opportunity } = application;
  const { labels } = checkout;

  if (submitted)
    return <VenueAcceptCheckoutFlow applicationId={applicationId} />;

  const summary = summaryFor(checkout.amount);

  async function handleAccept(paymentMethodId: string) {
    setError(null);
    try {
      await acceptMutation.mutateAsync({ applicationId, eSignature: signature, body: { paymentMethodId } });
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
      <CheckoutSection title="Deal Terms">
        <AcceptDealSummary deal={opportunity.deal} />
      </CheckoutSection>

      <CheckoutSection
        title="Payment Method"
        description={labels.paymentHint ?? undefined}
      >
        <div className="space-y-4">
          <ESignaturePanel value={signature} onChange={setSignature} />
          <StripePaymentForm
            session={checkout.session}
            submitLabel={labels.submitLabel}
            disabled={acceptMutation.isPending || !isValid}
            onSuccess={handleAccept}
          />
        </div>
      </CheckoutSection>
      {error && <p data-testid="payment-error" className="text-destructive text-sm">{error}</p>}
    </CheckoutLayout>
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
