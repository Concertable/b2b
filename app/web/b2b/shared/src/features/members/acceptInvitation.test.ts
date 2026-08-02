import { describe, expect, it, vi } from "vitest";
import { acceptInvitation } from "./acceptInvitation";
import type { Membership } from "@b2b/features/tenant";

describe("invitation acceptance", () => {
  it("selects the accepted tenant before navigating to member management", async () => {
    const membership: Membership = {
      tenantId: "accepted-tenant",
      legalName: "Accepted Venue",
      type: "Venue",
      role: "Staff",
    };
    const selectTenant = vi.fn();
    const navigate = vi.fn();

    await expect(
      acceptInvitation("invitation-id", {
        accept: vi.fn().mockResolvedValue(membership),
        selectTenant,
        navigate,
      }),
    ).resolves.toEqual(membership);

    expect(selectTenant).toHaveBeenCalledWith("accepted-tenant");
    expect(navigate).toHaveBeenCalledWith("/settings/members");
    expect(selectTenant.mock.invocationCallOrder[0]).toBeLessThan(
      navigate.mock.invocationCallOrder[0],
    );
  });
});
