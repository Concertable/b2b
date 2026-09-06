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

  it("resolves a single membership without a stored selection", () => {
    expect(resolveActiveMembership(memberships, "artist", undefined)).toEqual(
      memberships[2],
    );
  });

  it("requires selection when multiple memberships have no valid choice", () => {
    expect(hasPendingTenantChoice(memberships, "venue", "stale")).toBe(true);
    expect(resolveTenant(memberships, "venue", "stale")).toMatchObject({
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
