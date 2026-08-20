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
    type: "Venue",
    role: "Owner",
  },
  {
    tenantId: "venue-two",
    legalName: "Venue Two",
    type: "Venue",
    role: "Manager",
  },
  {
    tenantId: "artist-one",
    legalName: "Artist One",
    type: "Artist",
    role: "Staff",
  },
];

describe("tenant membership resolution", () => {
  it("filters memberships by tenant type", () => {
    expect(filterMembershipsByTenantType(memberships, "Venue")).toEqual(
      memberships.slice(0, 2),
    );
  });

  it("resolves the selected membership", () => {
    expect(
      resolveActiveMembership(memberships, "Venue", "venue-two"),
    ).toEqual(memberships[1]);
  });

  it("resolves a single membership without a stored selection", () => {
    expect(resolveActiveMembership(memberships, "Artist", undefined)).toEqual(
      memberships[2],
    );
  });

  it("requires selection when multiple memberships have no valid choice", () => {
    expect(hasPendingTenantChoice(memberships, "Venue", "stale")).toBe(true);
    expect(resolveTenant(memberships, "Venue", "stale")).toMatchObject({
      activeMembership: undefined,
      selectionRequired: true,
    });
  });
});
