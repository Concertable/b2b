import dayjs from "dayjs";
import { useVerification } from "../hooks/useVerification";
import { VerificationForm } from "../components/VerificationForm";
import { VERIFICATION_DOCUMENT_TYPE_LABELS } from "../types";

export function VerificationPage() {
  const { verification, isLoading } = useVerification();

  if (isLoading) return null;

  const status = verification?.status;
  const canSubmit = status === undefined || status === "rejected";

  return (
    <div className="mx-auto max-w-lg space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">Organisation verification</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          We confirm every organisation is a legitimate business before it can
          publish opportunities or be paid out. Upload a music licence, proof of
          address and company registration document.
        </p>
      </div>

      {verification && (
        <div className="border-border bg-card space-y-3 rounded-xl border p-4">
          {status === "pending" && (
            <p className="text-sm">
              Submitted{" "}
              <span className="font-medium">
                {dayjs(verification.submittedAt).format("D MMM YYYY")}
              </span>{" "}
              and awaiting review. We'll email you once it's been checked.
            </p>
          )}
          {status === "approved" && (
            <p className="text-sm">Your organisation is verified.</p>
          )}
          {status === "rejected" && (
            <p className="text-sm">
              Your last submission couldn't be approved
              {verification.rejectionReason
                ? `: ${verification.rejectionReason}`
                : "."}{" "}
              Upload new evidence below.
            </p>
          )}

          {verification.documents.length > 0 && (
            <ul className="text-muted-foreground space-y-1 text-sm">
              {verification.documents.map((document) => (
                <li key={`${document.documentType}-${document.uploadedAt}`}>
                  {VERIFICATION_DOCUMENT_TYPE_LABELS[document.documentType]} —{" "}
                  {dayjs(document.uploadedAt).format("D MMM YYYY")}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {canSubmit && <VerificationForm />}
    </div>
  );
}
