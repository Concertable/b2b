import dayjs from "dayjs";
import { ESignaturePanel, useESignature } from "@b2b/features/concerts";
import { Button } from "@concertable/web/shared/components/ui/button";
import { useSelfBillingAgreementQuery } from "../hooks/useSelfBillingAgreementQuery";
import { useGrantSelfBillingAgreementMutation } from "../hooks/useGrantSelfBillingAgreementMutation";
import { useDownloadSelfBillingAgreementMutation } from "../hooks/useDownloadSelfBillingAgreementMutation";
import { SelfBillingAgreementTerms } from "../components/SelfBillingAgreementTerms";

export function SelfBillingAgreementPage() {
  const { data: agreement, isLoading } = useSelfBillingAgreementQuery();
  const { signature, setSignature, isValid } = useESignature();
  const grant = useGrantSelfBillingAgreementMutation();
  const download = useDownloadSelfBillingAgreementMutation();

  if (isLoading || !agreement) return null;

  const showForm = agreement.actions.grant != null || agreement.actions.renew != null;

  return (
    <div className="mx-auto max-w-lg space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">Self-billing agreement</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          Concertable raises VAT invoices on your behalf for the supplies you make through the
          platform. This is the agreement that authorises it.
        </p>
      </div>

      <div className="border-border bg-card space-y-3 rounded-xl border p-4">
        {agreement.status === "None" && (
          <p className="text-sm">You don't have a self-billing agreement yet.</p>
        )}
        {agreement.status === "Active" && (
          <p className="text-sm">
            In force until{" "}
            <span className="font-medium">
              {dayjs(agreement.expiresAtUtc).format("D MMM YYYY")}
            </span>
            .
          </p>
        )}
        {agreement.status === "Expired" && (
          <p className="text-sm">
            Expired on{" "}
            <span className="font-medium">
              {dayjs(agreement.expiresAtUtc).format("D MMM YYYY")}
            </span>
            . Renew it to keep issuing invoices.
          </p>
        )}
        {agreement.actions.pdf != null && (
          <Button
            variant="outline"
            size="sm"
            disabled={download.isPending}
            onClick={() => download.mutate()}
            data-testid="download-self-billing"
          >
            Download PDF
          </Button>
        )}
      </div>

      {showForm && (
        <>
          <ESignaturePanel
            value={signature}
            onChange={setSignature}
            documentNoun="self-billing agreement"
          >
            <SelfBillingAgreementTerms />
          </ESignaturePanel>
          <Button
            disabled={!isValid || grant.isPending}
            onClick={() => grant.mutate(signature)}
            data-testid="grant-self-billing"
          >
            {agreement.status === "None" ? "Sign agreement" : "Renew agreement"}
          </Button>
        </>
      )}
    </div>
  );
}
