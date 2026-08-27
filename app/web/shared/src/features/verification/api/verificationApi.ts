import { apiClient } from "@concertable/shared/lib/apiClient";
import type { SubmitVerificationRequest } from "../schemas/submitVerificationRequestSchema";
import type { Verification, VerificationDocumentType } from "../types";

const BASE = "/organization/verification";

type FormDataValue = Parameters<FormData["append"]>[1];

// Multipart binds to the C# `VerificationDocumentType` enum by name, not through the camelCase JSON
// policy the GET response uses — so the wire token here is the server's PascalCase member name.
const DOCUMENT_TYPE_FIELD_VALUE: Record<VerificationDocumentType, string> = {
  licence: "Licence",
  proofOfAddress: "ProofOfAddress",
  companyRegistration: "CompanyRegistration",
};

const verificationApi = {
  get: async (): Promise<Verification | undefined> => {
    const { data, status } = await apiClient.get<Verification>(BASE);
    return status === 204 ? undefined : data;
  },

  submitDocuments: async (
    request: SubmitVerificationRequest,
  ): Promise<Verification> => {
    const formData = new FormData();
    request.documents.forEach(({ file, documentType }) => {
      formData.append("Files", file as unknown as FormDataValue);
      formData.append("DocumentTypes", DOCUMENT_TYPE_FIELD_VALUE[documentType]);
    });
    const { data } = await apiClient.post<Verification>(
      `${BASE}/documents`,
      formData,
    );
    return data;
  },
};

export default verificationApi;
