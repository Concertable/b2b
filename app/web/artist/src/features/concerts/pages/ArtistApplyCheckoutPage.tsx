import { useState } from "react";
import { useParams } from "@tanstack/react-router";
import { useMutation } from "@tanstack/react-query";
import { Skeleton } from "@concertable/web/shared/components/ui/skeleton";
import { ESignaturePanel, useApplyCheckoutQuery, useESignature } from "@concertable/b2b/web/shared/features/concerts";
import type { Checkout } from "@concertable/web/shared/features/concerts";
import applicationApi from "@concertable/b2b/web/shared/features/concerts/api/applicationApi";
import { CheckoutAwaiting } from "@concertable/web/shared/features/concerts/components/checkout/CheckoutAwaiting";
import { CheckoutLayout } from "@concertable/web/shared/features/concerts/components/checkout/CheckoutLayout";
import { CheckoutSection } from "@concertable/web/shared/features/concerts/components/checkout/CheckoutSection";
import { CheckoutEventBanner } from "@concertable/web/shared/features/concerts/components/checkout/CheckoutEventBanner";
import { OrderSummaryCard } from "@concertable/web/shared/features/concerts/components/checkout/OrderSummaryCard";
import { CheckoutSuccess } from "@concertable/web/shared/features/concerts/components/checkout/CheckoutSuccess";
import { StripePaymentForm } from "@concertable/web/shared/features/concerts/components/checkout/StripePaymentForm";
import { summaryFor } from "@concertable/b2b/web/shared/features/concerts/utils/acceptCheckoutFormat";

export function ArtistApplyCheckoutPage() {
  const { opportunityId } = useParams({ strict: false }) as {
    opportunityId: number;
  };
  const {
    data: checkout,
    isLoading,
    isError,
  } = useApplyCheckoutQuery(opportunityId);

  if (isLoading) return <CheckoutSkeleton />;
  if (isError || !checkout)
    return (
      <div className="text-destructive p-6">Could not start checkout.</div>
    );

  return <ArtistApplyCheckoutFlow opportunityId={opportunityId} checkout={checkout} />;
}

interface Props {
  opportunityId: number;
  checkout: Checkout;
}

export function ArtistApplyCheckoutFlow({ opportunityId, checkout }: Readonly<Props>) {
  const [submitted, setSubmitted] = useState(false);
  const { signature, setSignature, isValid } = useESignature();
  const { mutate, isPending, error } = useMutation({
    mutationFn: (paymentMethodId: string) =>
      applicationApi.applyToOpportunityWithPayment(opportunityId, paymentMethodId, signature),
    onSuccess: () => setSubmitted(true),
  });

  if (submitted)
    return (
      <CheckoutSuccess
        title="Application Submitted"
        description={
          <>
            Your card was saved and your application was sent to{" "}
            <span className="text-foreground font-medium">
              {checkout.payee.name}
            </span>
            . The venue will only charge if they accept.
          </>
        }
      />
    );

  if (isPending)
    return (
      <CheckoutAwaiting
        title="Submitting application"
        description={`Sending your application to ${checkout.payee.name}`}
        steps={[
          { label: "Card authorised", status: "done" },
          { label: "Submitting application", status: "active" },
        ]}
      />
    );

  const summary = summaryFor(checkout.amount);

  return (
    <CheckoutLayout
      banner={
        <CheckoutEventBanner
          title={checkout.payee.name}
          subtitle="Authorise card to apply"
          meta={checkout.payee.email ?? undefined}
        />
      }
      summary={
        <OrderSummaryCard
          title="Hire Fee"
          lines={summary.lines}
          total={summary.total}
        />
      }
    >
      <CheckoutSection
        title="Payment Method"
        description="The venue will only charge this card if your application is accepted."
      >
        <div className="space-y-4">
          <ESignaturePanel value={signature} onChange={setSignature} />
          <StripePaymentForm
            session={checkout.session}
            submitLabel="Authorise & Apply"
            disabled={!isValid}
            onSuccess={mutate}
          />
        </div>
      </CheckoutSection>
      {error && (
        <p data-testid="payment-error" className="text-destructive text-sm">{error.message}</p>
      )}
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
          <Skeleton className="h-44 w-full rounded-xl" />
        </div>
        <Skeleton className="h-60 w-full rounded-xl" />
      </div>
    </div>
  );
}
