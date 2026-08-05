import { useState, type ReactNode } from "react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import type { Deal } from "@b2b/features/deals";
import { eSignatureRequestSchema } from "@concertable/shared/features/concerts";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import { AcceptDealSummary } from "./AcceptDealSummary";
import { SignatureCanvas } from "./SignatureCanvas";

interface Props {
  /* Optional: when the caller has the deal it's shown conspicuously here. On the paid-apply
     checkout the deal fee is already shown alongside (OrderSummaryCard), so it may be omitted. */
  deal?: Deal;
  value: ESignatureRequest;
  onChange: (value: ESignatureRequest) => void;
  /* The document being e-signed, for the intent line. Defaults to the booking contract. */
  documentNoun?: string;
  /* Custom binding-terms body, shown conspicuously above the signature in place of the default
     deal-summary + platform-T&C block (e.g. the self-billing clause). */
  children?: ReactNode;
}

/* The signature step: the binding terms shown conspicuously at the point of signing, a required
   typed full name (the Advanced-tier attribution core), an optional drawn signature, and an
   explicit intent line. Nothing is pre-filled or pre-checked. */
export function ESignaturePanel({
  deal,
  value,
  onChange,
  documentNoun = "contract",
  children,
}: Readonly<Props>) {
  const [touched, setTouched] = useState(false);
  const nameResult =
    eSignatureRequestSchema.shape.signatoryName.safeParse(value.signatoryName);
  const nameError =
    touched && !nameResult.success ? nameResult.error.issues[0].message : null;

  return (
    <div className="border-border bg-card space-y-4 rounded-xl border p-4">
      <div className="space-y-3">
        <h3 className="text-sm font-semibold">What you are agreeing to</h3>
        {children ?? (
          <>
            {deal && <AcceptDealSummary deal={deal} />}
            <p className="text-muted-foreground text-xs">
              Cancellation and liability follow the Concertable platform Terms &amp; Conditions, which
              form part of this contract. By signing you confirm you have read and accept them.
            </p>
          </>
        )}
      </div>

      <Separator />

      <div className="space-y-2">
        <Label htmlFor="e-sign">Full name</Label>
        <Input
          id="e-sign"
          data-testid="e-sign"
          autoComplete="off"
          placeholder="Type your full name to sign"
          value={value.signatoryName}
          aria-invalid={nameError != null}
          onBlur={() => setTouched(true)}
          onChange={(e) => onChange({ ...value, signatoryName: e.target.value })}
        />
        {nameError && (
          <p className="text-destructive text-xs" data-testid="e-sign-error">
            {nameError}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label className="text-muted-foreground text-xs">Signature (optional)</Label>
        <SignatureCanvas
          onChange={(drawnSignatureImage) => onChange({ ...value, drawnSignatureImage })}
        />
      </div>

      <p className="text-muted-foreground text-xs">
        By signing, I agree to and e-sign this {documentNoun}.
      </p>
    </div>
  );
}
