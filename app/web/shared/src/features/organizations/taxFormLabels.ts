// UK org-form field labels. Region display copy is owned by the frontend — the backend serves region
// behaviour, not strings. UK-only today, so these are inlined rather than region-selected.
export const taxFormLabels = {
  sellerIdentifierLabel: "National Insurance number or UTR",
  sellerIdentifierHint: "Companies House number, or your UTR if you're a sole trader.",
  vatLabel: "VAT number",
  vatNumberPlaceholder: "GB123456789",
  musicLicenceLabel: "We hold the required live-music licence",
  musicLicenceHint:
    "A self-declaration for the live events you host. Holding the correct licence (such as PRS for Music) is your legal responsibility — we record this declaration but do not verify it.",
} as const;
