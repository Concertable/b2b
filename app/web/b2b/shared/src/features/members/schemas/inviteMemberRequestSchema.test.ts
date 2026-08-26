import { describe, expect, it } from "vitest";
import { inviteMemberRequestSchema } from "./inviteMemberRequestSchema";

describe("inviteMemberRequestSchema", () => {
  it("normalizes the email into the invitation request", () => {
    expect(
      inviteMemberRequestSchema.parse({
        email: "  MEMBER@EXAMPLE.COM ",
        role: "manager",
      }),
    ).toEqual({ email: "member@example.com", role: "manager" });
  });
});
