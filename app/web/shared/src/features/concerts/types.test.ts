import { describe, expect, it } from "vitest";
import { Opportunity, type OpportunityDraft } from "./types";

const draft: OpportunityDraft = {
  startDate: "2026-09-01",
  endDate: "2026-09-02",
  genres: ["rock"],
  deal: {
    $type: "flatFee",
    fee: 250,
    paymentMethod: "transfer",
  },
};

describe("Opportunity.toRequest", () => {
  it("omits read-only fields and carries an existing id", () => {
    expect(
      Opportunity.toRequest({
        ...draft,
        id: 42,
        venueId: 7,
        actions: {},
      }),
    ).toEqual({ ...draft, id: 42 });
  });

  it("leaves id absent for a new draft", () => {
    expect(Opportunity.toRequest(draft)).toEqual({
      ...draft,
      id: undefined,
    });
  });
});
