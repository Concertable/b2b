import { create } from "zustand";
import { immer } from "zustand/middleware/immer";
import type { ImageFile } from "@concertable/shared/types/image";
import type { Venue } from "@concertable/shared/features/venues/types";

export interface VenueState {
  tenantId: string | undefined;
  draft:
    | Pick<
        Venue,
        | "name"
        | "about"
        | "bannerUrl"
        | "avatar"
        | "county"
        | "town"
        | "latitude"
        | "longitude"
      >
    | undefined;
  banner: ImageFile | undefined;
  avatar: ImageFile | undefined;
  editMode: boolean;
  beginEdit: (
    tenantId: string | undefined,
    venue: NonNullable<VenueState["draft"]>,
  ) => void;
  endEdit: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setBanner: (banner: ImageFile) => void;
  setAvatar: (avatar: ImageFile) => void;
  setLocation: (
    latitude: number,
    longitude: number,
    county: string,
    town: string,
  ) => void;
}

export const useVenueStore = create<VenueState>()(
  immer((set) => ({
    tenantId: undefined,
    draft: undefined,
    banner: undefined,
    avatar: undefined,
    editMode: false,
    beginEdit: (tenantId, venue) =>
      set((state) => {
        state.tenantId = tenantId;
        state.draft = {
          name: venue.name,
          about: venue.about,
          bannerUrl: venue.bannerUrl,
          avatar: venue.avatar,
          county: venue.county,
          town: venue.town,
          latitude: venue.latitude,
          longitude: venue.longitude,
        };
        state.banner = undefined;
        state.avatar = undefined;
        state.editMode = true;
      }),
    endEdit: () =>
      set((state) => {
        state.tenantId = undefined;
        state.draft = undefined;
        state.banner = undefined;
        state.avatar = undefined;
        state.editMode = false;
      }),
    setName: (name) =>
      set((state) => {
        if (state.draft) state.draft.name = name;
      }),
    setAbout: (about) =>
      set((state) => {
        if (state.draft) state.draft.about = about;
      }),
    setBanner: (banner) =>
      set((state) => {
        if (!state.draft) return;
        state.draft.bannerUrl = banner.uri;
        state.banner = banner;
      }),
    setAvatar: (avatar) =>
      set((state) => {
        if (!state.draft) return;
        state.draft.avatar = avatar.uri;
        state.avatar = avatar;
      }),
    setLocation: (latitude, longitude, county, town) =>
      set((state) => {
        if (!state.draft) return;
        state.draft.latitude = latitude;
        state.draft.longitude = longitude;
        state.draft.county = county;
        state.draft.town = town;
      }),
  })),
);
