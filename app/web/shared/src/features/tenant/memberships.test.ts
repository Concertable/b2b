import { describe, expect, it } from "vitest";
import {
  filterMembershipsByTenantType,
  hasPendingTenantChoice,
  resolveActiveMembership,
  resolveTenant,
} from "./memberships";
import type { Membership } from "./types";

const memberships: ReadonlyArray<Membership> = [
  {
    tenantId: "venue-one",
    legalName: "Venue One",
    type: "venue",
    role: "owner",
  },
  {
    tenantId: "venue-two",
    legalName: "Venue Two",
    type: "venue",
    role: "manager",
  },
  {
    tenantId: "artist-one",
    legalName: "Artist One",
    type: "artist",
    role: "staff",
  },
];

describe("tenant membership resolution", () => {
  it("filters memberships by tenant type", () => {
    expect(filterMembershipsByTenantType(memberships, "venue")).toEqual(
      memberships.slice(0, 2),
    );
  });

  it("resolves the selected membership", () => {
    expect(
      resolveActiveMembership(memberships, "venue", "venue-two"),
    ).toEqual(memberships[1]);
  });

  it("resolves a single membership without persisting a selection", () => {
    expect(resolveActiveMembership(memberships, "artist", undefined)).toEqual(
      memberships[2],
    );
  });

  it("requires route selection when multiple memberships have no valid selection", () => {
    expect(hasPendingTenantChoice(memberships, "venue", "stale")).toBe(true);
    expect(resolveTenant(memberships, "venue", "stale")).toMatchObject({
      activeMembership: undefined,
      selectionRequired: true,
    });
  });
});
