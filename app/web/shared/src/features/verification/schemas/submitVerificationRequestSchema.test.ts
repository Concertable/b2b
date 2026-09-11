import { describe, expect, it } from "vitest";
import { submitVerificationRequestSchema } from "./submitVerificationRequestSchema";

const pdf = (name = "evidence.pdf") =>
  new File([new Uint8Array([1, 2, 3])], name, { type: "application/pdf" });

describe("submitVerificationRequestSchema", () => {
  it("accepts at least one PDF/JPEG/PNG document under the size cap", () => {
    const parsed = submitVerificationRequestSchema.safeParse({
      documents: [{ documentType: "licence", file: pdf() }],
    });

    expect(parsed.success).toBe(true);
  });

  it("rejects a submission with no documents", () => {
    const parsed = submitVerificationRequestSchema.safeParse({ documents: [] });

    expect(parsed.success).toBe(false);
    if (!parsed.success)
      expect(parsed.error.issues[0].message).toBe(
        "Attach at least one document.",
      );
  });

  it("rejects a disallowed content type", () => {
    const parsed = submitVerificationRequestSchema.safeParse({
      documents: [
        {
          documentType: "licence",
          file: new File(["x"], "notes.txt", { type: "text/plain" }),
        },
      ],
    });

    expect(parsed.success).toBe(false);
    if (!parsed.success)
      expect(parsed.error.issues[0].message).toBe(
        "Evidence must be a PDF, JPEG or PNG file.",
      );
  });
});
