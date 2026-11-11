import { describe, expect, it } from "vitest";
import { inviteMemberRequestSchema } from "./inviteMemberRequestSchema";

describe("inviteMemberRequestSchema", () => {
  it("normalizes the email into the invitation request", () => {
    expect(
      inviteMemberRequestSchema.parse({
        email: "  MEMBER@EXAMPLE.COM ",
        roleIds: ["11111111-1111-4111-8111-111111111111"],
      }),
    ).toEqual({ email: "member@example.com", roleIds: ["11111111-1111-4111-8111-111111111111"] });
  });
});
