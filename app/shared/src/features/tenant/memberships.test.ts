import { describe, expect, it } from "vitest";
import {
  filterMembershipsByProfile,
  hasPendingTenantChoice,
  resolveActiveMembership,
  resolveTenant,
} from "./memberships";
import type { Membership } from "./types";

const memberships: ReadonlyArray<Membership> = [
  {
    tenantId: "venue-one",
    legalName: "Venue One",
    businessProfiles: ["venueOperator"],
    role: "owner",
  },
  {
    tenantId: "venue-two",
    legalName: "Venue Two",
    businessProfiles: ["venueOperator"],
    role: "manager",
  },
  {
    tenantId: "artist-one",
    legalName: "Artist One",
    businessProfiles: ["artist"],
    role: "staff",
  },
];

describe("tenant membership resolution", () => {
  it("filters memberships by tenant type", () => {
    expect(filterMembershipsByProfile(memberships, "venueOperator")).toEqual(
      memberships.slice(0, 2),
    );
  });

  it("resolves the selected membership", () => {
    expect(
      resolveActiveMembership(memberships, "venueOperator", "venue-two"),
    ).toEqual(memberships[1]);
  });

  it("resolves a single membership without a stored selection", () => {
    expect(resolveActiveMembership(memberships, "artist", undefined)).toEqual(
      memberships[2],
    );
  });

  it("requires selection when multiple memberships have no valid choice", () => {
    expect(hasPendingTenantChoice(memberships, "venueOperator", "stale")).toBe(true);
    expect(resolveTenant(memberships, "venueOperator", "stale")).toMatchObject({
      activeMembership: undefined,
      selectionRequired: true,
    });
  });

  it("resolves across artist and venue memberships for the mobile B2B surface", () => {
    expect(resolveTenant(memberships, undefined, "artist-one")).toMatchObject({
      memberships,
      activeMembership: memberships[2],
      selectionRequired: false,
    });
  });
});
