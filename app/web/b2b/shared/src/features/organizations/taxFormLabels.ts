// UK org-form field labels. Region display copy is owned by the frontend — the backend serves region
// behaviour, not strings. UK-only today, so these are inlined rather than region-selected.
export const taxFormLabels = {
  sellerIdentifierLabel: "National Insurance number or UTR",
  sellerIdentifierHint: "Companies House number, or your UTR if you're a sole trader.",
  vatLabel: "VAT number",
  vatNumberPlaceholder: "GB123456789",
} as const;
