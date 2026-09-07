import { beforeEach, describe, expect, it } from "vitest";
import { artistKeys } from "./hooks/useArtistQuery";
import { useArtistStore } from "./store/useArtistStore";

const artist = {
  name: "Tenant A Artist",
  about: "About",
  bannerUrl: "banner",
  avatar: undefined,
  genres: [],
  county: "London",
  town: "London",
  latitude: 51.5,
  longitude: -0.1,
};

describe("artist tenant scope", () => {
  beforeEach(() => useArtistStore.getState().endEdit());

  it("uses a distinct active-profile cache key for each tenant", () => {
    expect(artistKeys.myForTenant("tenant-a")).not.toEqual(
      artistKeys.myForTenant("tenant-b"),
    );
    expect(artistKeys.myForTenant("tenant-a")).toEqual([
      "artist",
      "my",
      "tenant-a",
    ]);
  });

  it("records which tenant owns an edit draft", () => {
    useArtistStore.getState().beginEdit("tenant-a", artist);
    useArtistStore.getState().setName("Tenant A draft");

    expect(useArtistStore.getState()).toMatchObject({
      tenantId: "tenant-a",
      editMode: true,
      draft: { name: "Tenant A draft" },
    });

    useArtistStore.getState().endEdit();
    expect(useArtistStore.getState()).toMatchObject({
      tenantId: undefined,
      editMode: false,
      draft: undefined,
    });
  });
});
