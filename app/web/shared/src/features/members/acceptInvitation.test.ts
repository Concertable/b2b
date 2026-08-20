import { describe, expect, it, vi } from "vitest";
import { acceptInvitation } from "./acceptInvitation";
import type { Membership } from "@b2b/features/tenant";

describe("invitation acceptance", () => {
  it("waits for tenant selection before navigating", async () => {
    const membership: Membership = {
      tenantId: "accepted-tenant",
      legalName: "Accepted Venue",
      type: "venue",
      role: "staff",
    };
    let completeSelection: (() => void) | undefined;
    const selection = new Promise<void>((resolve) => {
      completeSelection = resolve;
    });
    const selectTenant = vi.fn().mockReturnValue(selection);
    const navigate = vi.fn();

    const acceptance = acceptInvitation("invitation-id", {
      accept: vi.fn().mockResolvedValue(membership),
      selectTenant,
      navigate,
    });

    await vi.waitFor(() =>
      expect(selectTenant).toHaveBeenCalledWith("accepted-tenant"),
    );
    expect(navigate).not.toHaveBeenCalled();

    completeSelection?.();
    await expect(acceptance).resolves.toEqual(membership);

    expect(navigate).toHaveBeenCalledWith("/settings/members");
    expect(selectTenant.mock.invocationCallOrder[0]).toBeLessThan(
      navigate.mock.invocationCallOrder[0],
    );
  });
});
