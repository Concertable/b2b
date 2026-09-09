import { beforeEach, describe, expect, it } from "vitest";
import { venueKeys } from "./hooks/useVenueQuery";
import { useVenueStore } from "./store/useVenueStore";

const venue = {
  name: "Tenant A Venue",
  about: "About",
  bannerUrl: "banner",
  avatar: undefined,
  county: "London",
  town: "London",
  latitude: 51.5,
  longitude: -0.1,
};

describe("venue tenant scope", () => {
  beforeEach(() => useVenueStore.getState().endEdit());

  it("uses a distinct active-profile cache key for each tenant", () => {
    expect(venueKeys.myForTenant("tenant-a")).not.toEqual(
      venueKeys.myForTenant("tenant-b"),
    );
    expect(venueKeys.myForTenant("tenant-a")).toEqual([
      "venue",
      "my",
      "tenant-a",
    ]);
  });

  it("records which tenant owns an edit draft", () => {
    useVenueStore.getState().beginEdit("tenant-a", venue);
    useVenueStore.getState().setName("Tenant A draft");

    expect(useVenueStore.getState()).toMatchObject({
      tenantId: "tenant-a",
      editMode: true,
      draft: { name: "Tenant A draft" },
    });

    useVenueStore.getState().endEdit();
    expect(useVenueStore.getState()).toMatchObject({
      tenantId: undefined,
      editMode: false,
      draft: undefined,
    });
  });
});
