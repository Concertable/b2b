import { describe, expect, it } from "vitest";
import { reportMessageRequestSchema } from "./reportMessageRequestSchema";

describe("reportMessageRequestSchema", () => {
  it("requires a category from the wire enum", () => {
    expect(reportMessageRequestSchema.safeParse({}).success).toBe(false);
    expect(reportMessageRequestSchema.safeParse({ category: "Spam" }).success).toBe(false);
    expect(reportMessageRequestSchema.parse({ category: "illegalContent" })).toEqual({
      category: "illegalContent",
    });
  });

  it("drops blank details and bounds them at 2000 characters", () => {
    expect(
      reportMessageRequestSchema.parse({ category: "spam", details: "   " }).details,
    ).toBeUndefined();
    expect(
      reportMessageRequestSchema.safeParse({ category: "spam", details: "x".repeat(2000) })
        .success,
    ).toBe(true);
    expect(
      reportMessageRequestSchema.safeParse({ category: "spam", details: "x".repeat(2001) })
        .success,
    ).toBe(false);
  });
});
