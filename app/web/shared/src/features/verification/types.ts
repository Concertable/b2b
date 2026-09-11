export type VerificationStatus = "pending" | "approved" | "rejected";

export type VerificationDocumentType =
  | "licence"
  | "proofOfAddress"
  | "companyRegistration";

export const VERIFICATION_DOCUMENT_TYPE_LABELS: Record<
  VerificationDocumentType,
  string
> = {
  licence: "Music licence",
  proofOfAddress: "Proof of address",
  companyRegistration: "Company registration",
};

export interface VerificationDocument {
  documentType: VerificationDocumentType;
  uploadedAt: string;
}

export interface Verification {
  status: VerificationStatus;
  rejectionReason?: string;
  submittedAt: string;
  documents: VerificationDocument[];
}
